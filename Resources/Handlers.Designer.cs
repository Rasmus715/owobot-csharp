﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace owobot_csharp.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Handlers {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Handlers() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("owobot_csharp.Resources.Handlers", typeof(Handlers).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Hellowo! I&apos;m owobot {0} - a bot that sends cute girls!
        ///
        ///I am written in C# using .NET 6, taking data from reddit, multi-threaded and fully compatible with group chats. Do not be afraid to send me 25-50 requests at a time, I can handle it!
        ///
        ///If you’re tired of reading and you want to see anime girls already, then you are here: /get
        ///By default, I will not send you NSFW content, however you can configure this here: /nsfw
        ///You can also change the language here: /language
        ///
        ///A few words about privacy - I sav [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string Info {
            get {
                return ResourceManager.GetString("Info", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Oa! Are you decided to change language?
        ///
        ///At this moment language is English.Here is what you can do:
        ////language_en to switch to English
        ////language_ru чтобы переключится на русский.
        /// </summary>
        internal static string LanguageInfo {
            get {
                return ResourceManager.GetString("LanguageInfo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Pam! Here&apos;s your picture from {0}
        ///Post title: {1}
        ///NSFW: {2}
        ///Link to the original: {3}
        ///Post Link: {4}.
        /// </summary>
        internal static string ReturnPic {
            get {
                return ResourceManager.GetString("ReturnPic", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Understood! From now, I&apos;ll answer you in english language!.
        /// </summary>
        internal static string SetLanguage {
            get {
                return ResourceManager.GetString("SetLanguage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Welcome, {0}!
        /// 
        ///To get more information type /info
        ///For a quick start, just type /get
        ///
        ///Also click on the slash icon at the keyboard to see command list..
        /// </summary>
        internal static string Start {
            get {
                return ResourceManager.GetString("Start", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to I&apos;m alive!
        ///
        ///Uptime: {0} Days {1}. Total requests: {2}
        ///NSFW for this chat: {3}
        ///Bot version: {4}.
        /// </summary>
        internal static string Status {
            get {
                return ResourceManager.GetString("Status", resourceCulture);
            }
        }
    }
}
