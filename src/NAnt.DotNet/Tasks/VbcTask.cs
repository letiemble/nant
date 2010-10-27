// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// Gerry Shaw (gerry_shaw@yahoo.com)
// Gert Driesen (driesen@users.sourceforge.net)
// Mike Krueger (mike@icsharpcode.net)
// Aaron A. Anderson (aaron@skypoint.com | aaron.anderson@farmcreditbank.com)
// Giuseppe Greco (giuseppe.greco@agamura.com)

using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.DotNet.Types;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Compiles Visual Basic.NET programs.
    /// </summary>
    /// <remarks>
    /// <note>
    /// In order to have <see cref="VbcTask" /> generate manifest resource names
    /// that match those generated by Microsoft Visual Studio.NET, the value of
    /// the <see cref="ResourceFileSet.Prefix" /> attribute of the &lt;<see cref="CompilerBase.ResourcesList" />&gt;
    /// element should match the "Root namespace" of the VB.NET project, and the 
    /// value of the <see cref="ResourceFileSet.DynamicPrefix" /> attribute 
    /// should be set to &quot;<see langword="false" />&quot;.
    /// </note>
    /// </remarks>
    /// <example>
    ///   <para>Example build file using this task.</para>
    ///   <code>
    ///     <![CDATA[
    /// <project name="Hello World" default="build" basedir=".">
    ///   <property name="basename" value="HelloWorld" />
    ///   <target name="clean">
    ///      <delete file="${basename}-vb.exe" failonerror="false" />
    ///      <delete file="${basename}-vb.pdb" failonerror="false" />
    ///   </target>
    ///   <target name="build">
    ///      <vbc target="exe" output="${basename}-vb.exe" rootnamespace="${basename}">
    ///         <imports>
    ///             <import namespace="System" />
    ///             <import namespace="System.Data" />
    ///         </imports>
    ///         <sources>
    ///            <include name="${basename}.vb" />
    ///         </sources>
    ///         <resources prefix="${basename}" dynamicprefix="true">
    ///             <include name="**/*.resx" />
    ///         </resources>
    ///         <references>
    ///             <include name="System.dll" />
    ///             <include name="System.Data.dll" />
    ///         </references>
    ///      </vbc>
    ///   </target>
    ///   <target name="rebuild" depends="clean, build" />
    /// </project>
    ///    ]]>
    ///   </code>
    /// </example>
    [TaskName("vbc")]
    [ProgramLocation(LocationType.FrameworkDir)]
    public class VbcTask : CompilerBase {
        #region Private Instance Fields

        private string _baseAddress;
        private DebugOutput _debugOutput = DebugOutput.None;
        private FileInfo _docFile;
        private bool _nostdlib;
        private string _optionCompare;
        private bool _optionExplicit;
        private bool _optionStrict;
        private bool _optionOptimize;
        private bool _removeintchecks;
        private string _rootNamespace;
        private string _platform;
        private NamespaceImportCollection _imports = new NamespaceImportCollection();

        // framework configuration settings
        private bool _supportsDocGeneration;
        private bool _supportsNoStdLib;
        private bool _supportsPlatform;

        #endregion Private Instance Fields

        #region Private Static Fields

        private static Regex _classNameRegex = new Regex(@"^((?<comment>/\*.*?(\*/|$))|[\s\.]+|Class\s+(?<class>\w+)|(?<keyword>\w+))*");
        private static Regex _namespaceRegex = new Regex(@"^((?<comment>/\*.*?(\*/|$))|[\s\.]+|Namespace\s+(?<namespace>(\w+(\.\w+)*)+)|(?<keyword>\w+))*");

        #endregion Private Static Fields
     
        #region Public Instance Properties

        /// <summary>
        /// The preferred base address at which to load a DLL. The default base 
        /// address for a DLL is set by the .NET Framework common language 
        /// runtime.
        /// </summary>
        /// <value>
        /// The preferred base address at which to load a DLL.
        /// </value>
        /// <remarks>
        /// This address must be specified as a hexadecimal number.
        /// </remarks>
        [TaskAttribute("baseaddress")]
        public string BaseAddress {
            get { return _baseAddress; }
            set { _baseAddress = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Specifies the type of debugging information generated by the 
        /// compiler. The default is <see cref="T:NAnt.DotNet.Types.DebugOutput.None" />.
        /// </summary>
        [TaskAttribute("debug")]
        public DebugOutput DebugOutput {
            get { return _debugOutput; }
            set { _debugOutput = value; }
        }

        /// <summary>
        /// No longer expose this to build authors. Use <see cref="DebugOutput" />
        /// instead.
        /// </summary>
        public override bool Debug {
            get { return DebugOutput != DebugOutput.None; }
            set { DebugOutput = DebugOutput.Enable; }
        }

        /// <summary>
        /// The name of the XML documentation file to generate. Only supported
        /// when targeting .NET 2.0 (or higher).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/doc:</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("doc")]
        public FileInfo DocFile {
            get { return _docFile; }
            set { _docFile = value; }
        }

        /// <summary>
        /// Specifies whether the <c>/imports</c> option gets passed to the 
        /// compiler.
        /// </summary>
        /// <value>
        /// The value of this attribute is a string that contains one or more 
        /// namespaces separated by commas.
        /// </value>
        /// <remarks>
        /// <a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfImportImportNamespaceFromSpecifiedAssembly.htm">See the Microsoft.NET Framework SDK documentation for details.</a>
        /// </remarks>
        /// <example>Example of an imports attribute
        /// <code><![CDATA[imports="Microsoft.VisualBasic, System, System.Collections, System.Data, System.Diagnostics"]]></code>
        /// </example>
        [TaskAttribute("imports")]
        [Obsolete("Use the <imports> element instead.", false)]
        public string ImportsString {
            set { 
                if (!StringUtils.IsNullOrEmpty(value)) {
                    string[] imports = value.Split(',');
                    foreach (string import in imports) {
                        Imports.Add(new NamespaceImport(import));
                    }
                }
            }
        }

        /// <summary>
        /// The namespaces to import.
        /// </summary>
        [BuildElement("imports")]
        public NamespaceImportCollection Imports {
            get { return _imports; }
            set { _imports = value; }
        }

        /// <summary>
        /// Instructs the compiler not to reference standard libraries
        /// (system.dll and VBC.RSP). The default is <see langword="false" />.
        /// Only supported when targeting .NET 2.0 (or higher).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/nostdlib</c> flag.
        /// </para>
        /// </remarks>
        [FrameworkConfigurable("nostdlib")]
        [TaskAttribute("nostdlib")]
        [BooleanValidator()]
        public bool NoStdLib {
            get { return _nostdlib; }
            set { _nostdlib = value; }
        }

        /// <summary>
        /// Specifies whether <c>/optioncompare</c> option gets passed to the 
        /// compiler.
        /// </summary>
        /// <value>
        /// <c>text</c>, <c>binary</c>, or an empty string.  If the value is 
        /// <see langword="false" /> or an empty string, the option will not be 
        /// passed to the compiler.
        /// </value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfOptioncompareSpecifyHowStringsAreCompared.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("optioncompare")]
        public string OptionCompare {
            get { return _optionCompare; }
            set { _optionCompare = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Specifies whether the <c>/optionexplicit</c> option gets passed to 
        /// the compiler. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the option should be passed to the compiler; 
        /// otherwise, <see langword="false" />.
        /// </value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfOptionexplicitRequireExplicitDeclarationOfVariables.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("optionexplicit")]
        [BooleanValidator()]
        public bool OptionExplicit {
            get { return _optionExplicit; }
            set { _optionExplicit = value; }
        }
        
        /// <summary>
        /// Specifies whether the <c>/optimize</c> option gets passed to the 
        /// compiler. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the option should be passed to the compiler; 
        /// otherwise, <see langword="false" />.
        /// </value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfoptimizeenabledisableoptimizations.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("optionoptimize")]
        [BooleanValidator()]
        public bool OptionOptimize {
            get { return _optionOptimize; }
            set { _optionOptimize = value; }
        }

        /// <summary>
        /// Specifies whether the <c>/optionstrict</c> option gets passed to 
        /// the compiler. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the option should be passed to the compiler; 
        /// otherwise, <see langword="false" />.
        /// </value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfOptionstrictEnforceStrictTypeSemantics.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("optionstrict")]
        [BooleanValidator()]
        public bool OptionStrict {
            get { return _optionStrict; }
            set { _optionStrict = value; }
        }

        /// <summary>
        /// Specifies which platform version of common language runtime (CLR)
        /// can run the output file.
        /// </summary>
        /// <value>
        /// The platform version of common language runtime (CLR) that can run
        /// the output file.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/platform</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("platform")]
        public string Platform {
            get { return _platform; }
            set { _platform = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Specifies whether the <c>/removeintchecks</c> option gets passed to 
        /// the compiler. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the option should be passed to the compiler; 
        /// otherwise, <see langword="false" />.
        /// </value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfRemoveintchecksRemoveInteger-OverflowChecks.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("removeintchecks")]
        [BooleanValidator()]
        public bool RemoveIntChecks {
            get { return _removeintchecks; }
            set { _removeintchecks = value; }
        }

        /// <summary>
        /// Specifies whether the <c>/rootnamespace</c> option gets passed to 
        /// the compiler.
        /// </summary>
        /// <value>
        /// The value of this attribute is a string that contains the root 
        /// namespace of the project.
        /// </value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfRootnamespace.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("rootnamespace")]
        public string RootNamespace {
            get { return _rootNamespace; }
            set { _rootNamespace = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Specifies whether the compiler for the active target framework
        /// supports generation of XML Documentation file. The default is 
        /// <see langword="false" />.
        /// </summary>
        [FrameworkConfigurable("supportsdocgeneration")]
        public bool SupportsDocGeneration {
            get { return _supportsDocGeneration; }
            set { _supportsDocGeneration = value; }
        }

        /// <summary>
        /// Specifies whether the compiler for the active target framework
        /// supports NOT referencing standard libraries (system.dll and VBC.RSP).
        /// The default is <see langword="false" />.
        /// </summary>
        [FrameworkConfigurable("supportsnostdlib")]
        public bool SupportsNoStdLib {
            get { return _supportsNoStdLib; }
            set { _supportsNoStdLib = value; }
        }

        /// <summary>
        /// Specifies whether the compiler for the active target framework
        /// supports limiting the platform on which the compiled code can run.
        /// The default is <see langword="false" />.
        /// </summary>
        [FrameworkConfigurable("supportsplatform")]
        public bool SupportsPlatform {
            get { return _supportsPlatform; }
            set { _supportsPlatform = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of CompilerBase

        /// <summary>
        /// Finds the correct namespace/classname for a resource file from the 
        /// given dependent source file, and ensure the <see cref="RootNamespace" />
        /// is prefixed.
        /// </summary>
        /// <param name="dependentFile">The file from which the resource linkage of the resource file should be determined.</param>
        /// <param name="resourceCulture">The culture of the resource file for which the resource linkage should be determined.</param>
        /// <returns>
        /// The namespace/classname of the source file matching the resource or
        /// <see langword="null" /> if the dependent source file does not exist.
        /// </returns>
        protected override ResourceLinkage GetResourceLinkage(string dependentFile, CultureInfo resourceCulture) {
            // determine resource linkage from dependent file
            ResourceLinkage resourceLinkage = base.GetResourceLinkage(dependentFile, resourceCulture);

            // check if resource linkage could be determined at all
            if (resourceLinkage != null) {
                // for VB.NET, the root namespace always needs to be used
                if (!StringUtils.IsNullOrEmpty(RootNamespace)) {
                    if (resourceLinkage.HasNamespaceName) {
                        resourceLinkage.NamespaceName = RootNamespace + "." + resourceLinkage.NamespaceName;
                    } else {
                        resourceLinkage.NamespaceName = RootNamespace;
                    }
                }
            }
            return resourceLinkage;
        }

        /// <summary>
        /// Writes conditional compilation constants to the specified
        /// <see cref="TextWriter" />.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter" /> to which the conditional compilation constants should be written.</param>
        protected override void WriteConditionalCompilationConstants(TextWriter writer) {
            if (Define != null) {
                string[] constants = Define.Split(',');
                foreach (string constant in constants) {
                    WriteOption(writer, "define", constant);
                }
            }
        }

        /// <summary>
        /// Writes the compiler options to the specified <see cref="TextWriter" />.
        /// </summary>
        /// <param name="writer"><see cref="TextWriter" /> to which the compiler options should be written.</param>
        protected override void WriteOptions(TextWriter writer) {
            // the base address for the DLL
            if (BaseAddress != null) {
                WriteOption(writer, "baseaddress", BaseAddress);
            }

            // XML documentation
            if (DocFile != null) {
                if (SupportsDocGeneration) {
                    WriteOption(writer, "doc", DocFile.FullName);
                } else {
                    Log(Level.Warning, ResourceUtils.GetString("String_CompilerDoesNotSupportXmlDoc"),
                        Project.TargetFramework.Description);
                }
            }

            if (NoStdLib) {
                if (SupportsNoStdLib) {
                    WriteOption(writer, "nostdlib");
                } else {
                    Log(Level.Warning, ResourceUtils.GetString("String_CompilerDoesNotSupportNoStdLib"),
                        Project.TargetFramework.Description);
                }
            }

            // platform
            if (Platform != null) {
                if (SupportsPlatform) {
                    WriteOption(writer, "platform", Platform);
                } else {
                    Log(Level.Warning, ResourceUtils.GetString("String_CompilerDoesNotSupportPlatform"),
                        Project.TargetFramework.Description);
                }
            }

            // win32res
            if (Win32Res != null) {
                WriteOption (writer, "win32resource", Win32Res.FullName);
            }

            // handle debug builds.
            switch (DebugOutput) {
                case DebugOutput.None:
                    break;
                case DebugOutput.Enable:
                    WriteOption(writer, "debug");
                    WriteOption(writer, "define", "DEBUG=True");
                    WriteOption(writer, "define", "TRACE=True");
                    break;
                case DebugOutput.Full:
                    WriteOption(writer, "debug");
                    break;
                case DebugOutput.PdbOnly:
                    WriteOption(writer, "debug", "pdbonly");
                    break;
                default:
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("NA2011"), DebugOutput), Location);
            }

            string imports = Imports.ToString();
            if (!StringUtils.IsNullOrEmpty(imports)) {
                WriteOption(writer, "imports", imports);
            }

            if (OptionCompare != null && OptionCompare.ToUpper(CultureInfo.InvariantCulture) != "FALSE") {
                WriteOption(writer, "optioncompare", OptionCompare);
            }

            if (OptionExplicit) {
                WriteOption(writer, "optionexplicit");
            }

            if (OptionStrict) {
                WriteOption(writer, "optionstrict");
            }

            if (RemoveIntChecks) {
                WriteOption(writer, "removeintchecks");
            }

            if (OptionOptimize) {
                WriteOption(writer, "optimize");
            }

            if (RootNamespace != null) {
                WriteOption(writer, "rootnamespace", RootNamespace);
            }

            if (Project.TargetFramework.Family == "netcf") {
                WriteOption(writer, "netcf");
                WriteOption(writer, "sdkpath", Project.TargetFramework.
                    FrameworkAssemblyDirectory.FullName);
            }
        }

        /// <summary>
        /// Determines whether compilation is needed.
        /// </summary>
        protected override bool NeedsCompiling() {
            if (base.NeedsCompiling()) {
                return true;
            }

            if (DocFile != null && SupportsDocGeneration) {
                if (!DocFile.Exists) {
                    Log(Level.Verbose, ResourceUtils.GetString("String_DocFileDoesNotExist"),
                        DocFile.FullName);
                    return true;
                }
            }

            return base.NeedsCompiling();
        }

        /// <summary>
        /// Gets the file extension required by the current compiler.
        /// </summary>
        /// <value>
        /// For the VB.NET compiler, the file extension is always <c>vb</c>.
        /// </value>
        public override string Extension {
            get { return "vb"; }
        }

        /// <summary>
        /// Gets the class name regular expression for the language of the 
        /// current compiler.
        /// </summary>
        /// <value>
        /// Class name regular expression for the language of the current 
        /// compiler.
        /// </value>
        protected override Regex ClassNameRegex {
            get { return _classNameRegex; }
        }

        /// <summary>
        /// Gets the namespace regular expression for the language of the 
        /// current compiler.
        /// </summary>
        /// <value>
        /// Namespace regular expression for the language of the current 
        /// compiler.
        /// </value>
        protected override Regex NamespaceRegex {
            get { return _namespaceRegex; }
        }

        #endregion Override implementation of CompilerBase
    }
}
