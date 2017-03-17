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

using Emmellsoft.IoT.Rpi.SenseHat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligentKioskSample
{
    class SenseHatInputManager
    {
        private static SenseHatInputManager instance = new SenseHatInputManager();

        public event EventHandler LeftPressed;
        public event EventHandler RightPressed;
        public event EventHandler UpPressed;
        public event EventHandler DownPressed;
        public event EventHandler EnterPressed;

        private SenseHatInputManager()
        {
        }

        public static SenseHatInputManager Instance { get { return instance; } }

        public void Start()
        {
            Task.Run(async () =>
            {
                ISenseHat senseHat = await SenseHatFactory.GetSenseHat();

                while (true)
                {
                    if (senseHat.Joystick.Update()) 
                    {
                        if (senseHat.Joystick.LeftKey == KeyState.Pressed)
                        {
                            LeftPressed?.Invoke(this, EventArgs.Empty);
                        }
                        else if (senseHat.Joystick.RightKey == KeyState.Pressed)
                        {
                            RightPressed?.Invoke(this, EventArgs.Empty);
                        }
                        else if (senseHat.Joystick.UpKey == KeyState.Pressed)
                        {
                            UpPressed?.Invoke(this, EventArgs.Empty);
                        }
                        else if (senseHat.Joystick.DownKey == KeyState.Pressed)
                        {
                            DownPressed?.Invoke(this, EventArgs.Empty);
                        }
                        else if (senseHat.Joystick.EnterKey == KeyState.Pressed)
                        {
                            EnterPressed?.Invoke(this, EventArgs.Empty);
                        }
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(2));
                }
            });
        }
    }
}
