// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Cognitive Services: http://www.microsoft.com/cognitive
// 
// Microsoft Cognitive Services Github:
// https://github.com/Microsoft/Cognitive
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.AI.MachineLearning;
using Windows.Media;
using Windows.Storage;

/// <summary>
/// See Custom Vision ONNX UWP sample https://github.com/Azure-Samples/Custom-Vision-ONNX-UWP/blob/master/VisionApp/ONNXModel.cs
/// </summary>
namespace IntelligentKioskSample.Views.CustomVision
{
    public sealed class CustomVisionModelInput
    {
        public VideoFrame data { get; set; }
    }

    public sealed class CustomVisionModelOutput
    {
        // The label returned by the model
        public TensorString classLabel = TensorString.Create(new long[] { 1, 1 });

        // The loss returned by the model
        public IList<IDictionary<string, float>> loss = new List<IDictionary<string, float>>();

        public List<Tuple<string, float>> GetPredictionResult()
        {
            List<Tuple<string, float>> result = new List<Tuple<string, float>>();
            foreach (IDictionary<string, float> dict in loss)
            {
                foreach (var item in dict)
                {
                    result.Add(new Tuple<string, float>(item.Key, item.Value));
                }
            }
            return result;
        }
    }

    public sealed class CustomVisionModel
    {
        private LearningModel _learningModel = null;
        private LearningModelSession _session;
        public int InputImageWidth { get; private set; }
        public int InputImageHeight { get; private set; }


        // Create a model from an ONNX 1.2 file
        public static async Task<CustomVisionModel> CreateONNXModel(StorageFile file)
        {
            LearningModel learningModel = null;
            try
            {
                learningModel = await LearningModel.LoadFromStorageFileAsync(file);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            var inputFeatures = learningModel.InputFeatures;
            ImageFeatureDescriptor inputImageDescription = inputFeatures?.FirstOrDefault(feature => feature.Kind == LearningModelFeatureKind.Image) as ImageFeatureDescriptor;
            uint inputImageWidth = 0, inputImageHeight = 0;
            if (inputImageDescription != null)
            {
                inputImageHeight = inputImageDescription.Height;
                inputImageWidth = inputImageDescription.Width;
            }

            return new CustomVisionModel()
            {
                _learningModel = learningModel,
                _session = new LearningModelSession(learningModel),
                InputImageWidth = (int)inputImageWidth,
                InputImageHeight = (int)inputImageHeight
            };
        }

        /// <summary>
        /// Evaluate the model
        /// </summary>
        /// <param name="input">The VideoFrame to evaluate</param>
        /// <returns></returns>
        public async Task<CustomVisionModelOutput> EvaluateAsync(CustomVisionModelInput input)
        {
            var output = new CustomVisionModelOutput();
            var binding = new LearningModelBinding(_session);
            binding.Bind("data", input.data);
            binding.Bind("classLabel", output.classLabel);
            binding.Bind("loss", output.loss);
            LearningModelEvaluationResult result = await _session.EvaluateAsync(binding, "0");
            return output;
        }
    }
}