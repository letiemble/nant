// NAnt - A .NET build tool
// Copyright (C) 2004 Thomas Strauss (strausst@arcor.de)
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// Thomas Strauss (strausst@arcor.de)

using System;   
using System.IO;

using NUnit.Framework;

using NAnt.Core;

using Tests.NAnt.Core;
using Tests.NAnt.Core.Util;
using Tests.NAnt.VisualCpp;

namespace Tests.NAnt.VSNet.Tasks {
    [TestFixture]
    public class VCProjectTest : VisualCppTestBase {
        private string _objDir;
        private string _sourceDir;
        private string _sourcePathName;
        private string _sourceErrorPathName;
        private string _test_build;
        private string _vcProject;
        private string _vcProjectPathName;

        private const string _helloWorld_cpp = @"
            #include <windows.h>
            extern "+"\"C\""+@" void __declspec( dllexport ) init() {}
            ";

        private const string _error_cpp = @"xxx";

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _objDir = CreateTempDir("objs");
            _sourceDir = CreateTempDir("src");
            _sourcePathName = CreateTempFile(Path.Combine(_sourceDir, "HelloWorld.cpp"), _helloWorld_cpp);
            _sourceErrorPathName = CreateTempFile(Path.Combine(_sourceDir, "NotBuild.cpp"), _error_cpp);

            _vcProject = @"<?xml version='1.0' encoding='Windows-1252'?>
                <VisualStudioProject
                    ProjectType='Visual C++'
                    Version='7.10'
                    Name='HelloWorld'
                    ProjectGUID='{8A1ABDBF-6B54-4134-864B-6A1D144D1F33}'
                    Keyword='Win32Proj'>
                    <Platforms>
                        <Platform Name='Win32'/>
                    </Platforms>
                    <Configurations>
                        <Configuration
                            Name='Debug|Win32'
                            OutputDirectory='"+Path.GetFullPath(_objDir)+@"'
                            IntermediateDirectory='"+Path.GetFullPath(_objDir)+@"'
                            ConfigurationType='2'
                            CharacterSet='2'>
                            <Tool
                                Name='VCCLCompilerTool'
                                Optimization='0'
                                PreprocessorDefinitions='WIN32;_DEBUG;_WINDOWS;_USRDLL;WIN32_EXPORTS'
                                MinimalRebuild='TRUE'
                                BasicRuntimeChecks='3'
                                RuntimeLibrary='1'
                                EnableEnhancedInstructionSet='1'
                                UsePrecompiledHeader='0'
                                WarningLevel='3'
                                Detect64BitPortabilityProblems='TRUE'
                                DebugInformationFormat='4' />
                            <Tool Name='VCCustomBuildTool' />
                            <Tool
                                Name='VCLinkerTool'
                                OutputFile='$(OutDir)/HelloWorld.dll'
                                LinkIncremental='2'
                                GenerateDebugInformation='TRUE'
                                ProgramDatabaseFile='$(OutDir)/HelloWorld.pdb'
                                SubSystem='2'
                                ImportLibrary='$(OutDir)/HelloWorld.lib'
                                TargetMachine='1' />
                            <Tool Name='VCMIDLTool' />
                            <Tool Name='VCPostBuildEventTool' />
                            <Tool Name='VCPreBuildEventTool' />
                            <Tool Name='VCPreLinkEventTool' />
                            <Tool Name='VCResourceCompilerTool' />
                            <Tool Name='VCWebServiceProxyGeneratorTool' />
                            <Tool Name='VCXMLDataGeneratorTool' />
                            <Tool Name='VCWebDeploymentTool' />
                            <Tool Name='VCManagedWrapperGeneratorTool' />
                            <Tool Name='VCAuxiliaryManagedWrapperGeneratorTool' />
                        </Configuration>
                    </Configurations>
                    <References>
                    </References>
                    <Files>
                        <Filter
                            Name='Source Files'
                            Filter='cpp;c;cxx;def;odl;idl;hpj;bat;asm;asmx'
                            UniqueIdentifier='{4FC737F1-C7A5-4376-A066-2A32D752A2FF}'>
                            <File
                                RelativePath='"+Path.GetFileName(_sourcePathName)+@"'>
                            </File>
                            <File
                                RelativePath='"+Path.GetFileName(_sourceErrorPathName)+@"'>
                                <FileConfiguration
                                    Name='Debug|Win32'
                                    ExcludedFromBuild='TRUE'>
                                    <Tool
                                        Name='VCCLCompilerTool'/>
                                </FileConfiguration>
                            </File>
                        </Filter>
                        <Filter
                            Name='Header Files'
                            Filter='h;hpp;hxx;hm;inl;inc;xsd'
                            UniqueIdentifier='{93995380-89BD-4b04-88EB-625FBE52EBFB}'>
                        </Filter>
                        <Filter
                            Name='Resource Files'
                            Filter='rc;ico;cur;bmp;dlg;rc2;rct;bin;rgs;gif;jpg;jpeg;jpe;resx'
                            UniqueIdentifier='{67DA6AB6-F800-4c08-8B7A-83BB121AAD01}'>
                        </Filter>
                    </Files>
                    <Globals>
                    </Globals>
                </VisualStudioProject>";
                
            _vcProjectPathName = CreateTempFile(Path.Combine(_sourceDir, "HelloWorld.vcproj"), _vcProject);
            _vcProjectPathName = _vcProjectPathName.Replace("\\", "/");

            _test_build = @"<?xml version='1.0'?>
                <project default='build' basedir='"+Path.GetDirectoryName(_vcProjectPathName)+@"'>
                    <target name='build'>
                        <solution configuration='debug' verbose='true'>
                            <projects basedir='"+Path.GetDirectoryName(_vcProjectPathName)+@"'>
                                <include name='"+Path.GetFileName(_vcProjectPathName)+@"'/>
                            </projects>
                        </solution>
                    </target>    
                </project>";
                
        }
        
        /// <summary>
        /// Tests excluded files are actually ignored.
        /// </summary>
        [Test]
        public void Test_HelloWorldCompile_ExcludeFile() {
            if (CanCompileAndLink) {
                string result = RunBuild(_test_build);
                Assertion.Assert("dll file not created.", File.Exists(Path.Combine(_objDir, "HelloWorld.dll")));
            }
        }
    }
}