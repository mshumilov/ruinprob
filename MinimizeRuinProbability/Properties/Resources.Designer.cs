﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MinimizeRuinProbability.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("MinimizeRuinProbability.Properties.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to 50 0.005346265174752670 0.003283385442263650
        ///51 0.005810688210135220 0.003544801480660440
        ///52 0.006264310709811210 0.003795760877521360
        ///53 0.006717933209487190 0.004015350349774660
        ///54 0.007171555709163170 0.004234939822027960
        ///55 0.007657579815958870 0.004475442577353010
        ///56 0.008176005529874280 0.004747315257285670
        ///57 0.008683630708083120 0.005071471144897680
        ///58 0.009180455350585390 0.005458366881724930
        ///59 0.009677279993087660 0.005897545826231530
        ///60 0.010228107314122800 0.006399464619953360
        ///61 0.0 [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string ageprobs {
            get {
                return ResourceManager.GetString("ageprobs", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 0.082509 0.0402696529 0.021409 0.0069605649 0.0007344180 2.75 0.000 4.00
        ///1000 100
        ///0 50.
        /// </summary>
        internal static string control {
            get {
                return ResourceManager.GetString("control", resourceCulture);
            }
        }
    }
}
