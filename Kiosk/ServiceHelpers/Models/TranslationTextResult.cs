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

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceHelpers.Models
{
    [DataContract]
    public class TranslationTextResult
    {
        /// <summary>
        /// Detected language.
        /// </summary>
        [DataMember(Name = "detectedLanguage")]
        public DetectedLanguageResult DetectedLanguage { get; set; }

        /// <summary>
        /// An array of translation results. The size of the array matches the number of target languages specified through the to query parameter. 
        /// </summary>
        [DataMember(Name = "translations")]
        public List<Translation> Translations { get; set; }
    }

    [DataContract]
    public class DetectedLanguageResult
    {
        /// <summary>
        /// Code of the detected language.
        /// </summary>
        [DataMember(Name = "language")]
        public string Language { get; set; }

        /// <summary>
        /// A float value indicating the confidence in the result. 
        /// The score is between zero and one and a low score indicates a low confidence.
        /// </summary>
        [DataMember(Name = "score")]
        public float Score { get; set; }

        /// <summary>
        /// A boolean value which is true if the detected language is one of the languages supported for text translation.
        /// </summary>
        [DataMember(Name = "isTranslationSupported")]
        public bool? IsTranslationSupported { get; set; }

        /// <summary>
        /// A boolean value which is true if the detected language is one of the languages supported for transliteration.
        /// </summary>
        [DataMember(Name = "isTransliterationSupported")]
        public bool? IsTransliterationSupported { get; set; }

        /// <summary>
        /// List of other possible languages.
        /// </summary>
        [DataMember(Name = "alternatives")]
        public List<DetectedLanguageResult> Alternatives { get; set; }
    }

    [DataContract]
    public class Translation
    {
        /// <summary>
        /// A string giving the translated text.
        /// </summary>
        [DataMember(Name = "text")]
        public string Text { get; set; }

        /// <summary>
        /// A string representing the language code of the target language.
        /// </summary>
        [DataMember(Name = "to")]
        public string To { get; set; }

        /// <summary>
        /// An object giving the translated text in the script specified by the toScript parameter.
        /// </summary>
        [DataMember(Name = "transliteration")]
        public Transliteration Transliteration { get; set; }
    }

    [DataContract]
    public class Transliteration
    {
        /// <summary>
        /// A string specifying the target script.
        /// </summary>
        [DataMember(Name = "script")]
        public string Script { get; set; }

        /// <summary>
        /// A string giving the translated text in the target script.
        /// </summary>
        [DataMember(Name = "text")]
        public string Text { get; set; }
    }

    [DataContract]
    public class SupportedLanguages
    {
        /// <summary>
        /// Provides languages supported to translate text from one language to another language 
        /// </summary>
        [DataMember(Name = "translation")]
        public Dictionary<string, Language> Translation { get; set; }

        /// <summary>
        /// Provides capabilities for converting text in one language from one script to another script 
        /// </summary>
        [DataMember(Name = "transliteration")]
        public Dictionary<string, LanguageTransliteration> Transliteration { get; set; }

        /// <summary>
        /// Provides language pairs for which Dictionary operations return data 
        /// </summary>
        [DataMember(Name = "dictionary")]
        public Dictionary<string, LanguageDictionary> Dictionary { get; set; }
    }

    [DataContract]
    public class Language
    {
        /// <summary>
        /// Display name of the language in the locale requested via Accept-Language header.
        /// </summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Display name of the language in the locale native for this language.
        /// </summary>
        [DataMember(Name = "nativeName")]
        public string NativeName { get; set; }

        /// <summary>
        /// Directionality, which is rtl for right-to-left languages or ltr for left-to-right languages.
        /// </summary>
        [DataMember(Name = "dir")]
        public string Dir { get; set; }

        /// <summary>
        /// Code identifying the language.
        /// </summary>
        [DataMember(Name = "code")]
        public string Code { get; set; }
    }

    [DataContract]
    public class LanguageTransliteration : Language
    {
        /// <summary>
        /// List of scripts to convert from.
        /// </summary>
        [DataMember(Name = "scripts")]
        public List<Script> Scripts { get; set; }
    }

    [DataContract]
    public class LanguageDictionary : Language
    {
        /// <summary>
        /// List of languages with alternative translations and examples for the query expressed in the source language. 
        /// </summary>
        [DataMember(Name = "translations")]
        public List<Language> Translations { get; set; }
    }

    [DataContract]
    public class Script : Language
    {
        /// <summary>
        /// List of scripts available to convert text to.
        /// </summary>
        [DataMember(Name = "toScripts")]
        public List<Language> ToScripts { get; set; }
    }

    [DataContract]
    public class LookupLanguage
    {
        /// <summary>
        /// A string giving the normalized form of the source term. 
        /// For example, if the request is "JOHN", the normalized form will be "john".
        /// </summary>
        [DataMember(Name = "normalizedSource")]
        public string NormalizedSource { get; set; }

        /// <summary>
        /// A string giving the source term in a form best suited for end-user display. 
        /// For example, if the input is "JOHN", the display form will reflect the usual spelling of the name: "John".
        /// </summary>
        [DataMember(Name = "displaySource")]
        public string DisplaySource { get; set; }

        /// <summary>
        /// A list of translations for the source term.
        /// </summary>
        [DataMember(Name = "translations")]
        public List<LookupTranslations> Translations { get; set; }
    }

    [DataContract]
    public class LookupTranslations
    {
        /// <summary>
        /// A string giving the normalized form of this term in the target language.
        /// </summary>
        [DataMember(Name = "normalizedTarget")]
        public string NormalizedTarget { get; set; }

        /// <summary>
        /// A string giving the term in the target language and in a form best suited for end-user display. 
        /// For example, a proper noun like "Juan" will have normalizedTarget = "juan" and displayTarget = "Juan".
        /// </summary>
        [DataMember(Name = "displayTarget")]
        public string DisplayTarget { get; set; }

        /// <summary>
        /// A string associating this term with a part-of-speech tag.
        /// </summary>
        [DataMember(Name = "posTag")]
        public string PosTag { get; set; }

        /// <summary>
        /// A value between 0.0 and 1.0 which represents the "confidence" of that translation pair. 
        /// The sum of confidence scores for one source word may or may not sum to 1.0.
        /// </summary>
        [DataMember(Name = "confidence")]
        public float Confidence { get; set; }

        /// <summary>
        /// A string giving the word to display as a prefix of the translation. 
        /// Currently, this is the gendered determiner of nouns, in languages that have gendered determiners. 
        /// For example, the prefix of the Spanish word "mosca" is "la", since "mosca" is a feminine noun in Spanish. 
        /// </summary>
        [DataMember(Name = "prefixWord")]
        public string PrefixWord { get; set; }

        /// <summary>
        /// A list of "back translations" of the target.
        /// </summary>
        [DataMember(Name = "backTranslations")]
        public List<BackTranslation> BackTranslations { get; set; }
    }

    [DataContract]
    public class BackTranslation
    {
        /// <summary>
        /// A string giving the normalized form of the source term that is a back-translation of the target.
        /// </summary>
        [DataMember(Name = "normalizedText")]
        public string NormalizedText { get; set; }

        /// <summary>
        /// A string giving the source term that is a back-translation of the target in a form best suited for end-user display.
        /// </summary>
        [DataMember(Name = "displayText")]
        public string DisplayText { get; set; }

        /// <summary>
        /// An integer representing the number of examples that are available for this translation pair.
        /// </summary>
        [DataMember(Name = "numExamples")]
        public int NumExamples { get; set; }

        /// <summary>
        /// An integer representing the frequency of this translation pair in the data.
        /// </summary>
        [DataMember(Name = "frequencyCount")]
        public int FrequencyCount { get; set; }
    }
}
