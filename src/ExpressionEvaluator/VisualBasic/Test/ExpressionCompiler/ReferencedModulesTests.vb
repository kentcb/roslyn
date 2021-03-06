﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Linq
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
Imports Microsoft.DiaSymReader
Imports Roslyn.Test.PdbUtilities
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class ReferencedModulesTests
        Inherits ExpressionCompilerTestBase

        ''' <summary>
        ''' MakeAssemblyReferences should drop unreferenced assemblies.
        ''' </summary>
        <WorkItem(1141029)>
        <Fact>
        Public Sub AssemblyDuplicateReferences()
            Const sourceA =
"Public Class A
End Class"
            Const sourceB =
"Public Class B
    Public F As New A()
End Class"
            Const sourceC =
"Class C
    Public F As New B()
    Shared Sub M()
    End Sub
End Class"
            ' Assembly A, multiple versions, strong name.
            Dim assemblyNameA = ExpressionCompilerUtilities.GenerateUniqueName()
            Dim publicKeyA = ImmutableArray.CreateRange(Of Byte)({&H00, &H24, &H00, &H00, &H04, &H80, &H00, &H00, &H94, &H00, &H00, &H00, &H06, &H02, &H00, &H00, &H00, &H24, &H00, &H00, &H52, &H53, &H41, &H31, &H00, &H04, &H00, &H00, &H01, &H00, &H01, &H00, &HED, &HD3, &H22, &HCB, &H6B, &HF8, &HD4, &HA2, &HFC, &HCC, &H87, &H37, &H04, &H06, &H04, &HCE, &HE7, &HB2, &HA6, &HF8, &H4A, &HEE, &HF3, &H19, &HDF, &H5B, &H95, &HE3, &H7A, &H6A, &H28, &H24, &HA4, &H0A, &H83, &H83, &HBD, &HBA, &HF2, &HF2, &H52, &H20, &HE9, &HAA, &H3B, &HD1, &HDD, &HE4, &H9A, &H9A, &H9C, &HC0, &H30, &H8F, &H01, &H40, &H06, &HE0, &H2B, &H95, &H62, &H89, &H2A, &H34, &H75, &H22, &H68, &H64, &H6E, &H7C, &H2E, &H83, &H50, &H5A, &HCE, &H7B, &H0B, &HE8, &HF8, &H71, &HE6, &HF7, &H73, &H8E, &HEB, &H84, &HD2, &H73, &H5D, &H9D, &HBE, &H5E, &HF5, &H90, &HF9, &HAB, &H0A, &H10, &H7E, &H23, &H48, &HF4, &HAD, &H70, &H2E, &HF7, &HD4, &H51, &HD5, &H8B, &H3A, &HF7, &HCA, &H90, &H4C, &HDC, &H80, &H19, &H26, &H65, &HC9, &H37, &HBD, &H52, &H81, &HF1, &H8B, &HCD})
            Dim compilationAS1 = CreateCompilation(
                New AssemblyIdentity(assemblyNameA, New Version(1, 1, 1, 1), cultureName:="", publicKeyOrToken:=publicKeyA, hasPublicKey:=True),
                {sourceA},
                references:={MscorlibRef},
                options:=TestOptions.DebugDll.WithDelaySign(True))
            Dim referenceAS1 = compilationAS1.EmitToImageReference()
            Dim identityAS1 = referenceAS1.GetAssemblyIdentity()
            Dim compilationAS2 = CreateCompilation(
                New AssemblyIdentity(assemblyNameA, New Version(2, 1, 1, 1), cultureName:="", publicKeyOrToken:=publicKeyA, hasPublicKey:=True),
                {sourceA},
                references:={MscorlibRef},
                options:=TestOptions.DebugDll.WithDelaySign(True))
            Dim referenceAS2 = compilationAS2.EmitToImageReference()
            Dim identityAS2 = referenceAS2.GetAssemblyIdentity()

            ' Assembly B, multiple versions, strong name.
            Dim assemblyNameB = ExpressionCompilerUtilities.GenerateUniqueName()
            Dim publicKeyB = ImmutableArray.CreateRange(Of Byte)({&H00, &H24, &H00, &H00, &H04, &H80, &H00, &H00, &H94, &H00, &H00, &H00, &H06, &H02, &H00, &H00, &H00, &H24, &H00, &H00, &H53, &H52, &H41, &H31, &H00, &H04, &H00, &H00, &H01, &H00, &H01, &H00, &HED, &HD3, &H22, &HCB, &H6B, &HF8, &HD4, &HA2, &HFC, &HCC, &H87, &H37, &H04, &H06, &H04, &HCE, &HE7, &HB2, &HA6, &HF8, &H4A, &HEE, &HF3, &H19, &HDF, &H5B, &H95, &HE3, &H7A, &H6A, &H28, &H24, &HA4, &H0A, &H83, &H83, &HBD, &HBA, &HF2, &HF2, &H52, &H20, &HE9, &HAA, &H3B, &HD1, &HDD, &HE4, &H9A, &H9A, &H9C, &HC0, &H30, &H8F, &H01, &H40, &H06, &HE0, &H2B, &H95, &H62, &H89, &H2A, &H34, &H75, &H22, &H68, &H64, &H6E, &H7C, &H2E, &H83, &H50, &H5A, &HCE, &H7B, &H0B, &HE8, &HF8, &H71, &HE6, &HF7, &H73, &H8E, &HEB, &H84, &HD2, &H73, &H5D, &H9D, &HBE, &H5E, &HF5, &H90, &HF9, &HAB, &H0A, &H10, &H7E, &H23, &H48, &HF4, &HAD, &H70, &H2E, &HF7, &HD4, &H51, &HD5, &H8B, &H3A, &HF7, &HCA, &H90, &H4C, &HDC, &H80, &H19, &H26, &H65, &HC9, &H37, &HBD, &H52, &H81, &HF1, &H8B, &HCD})
            Dim compilationBS1 = CreateCompilation(
                New AssemblyIdentity(assemblyNameB, New Version(1, 1, 1, 1), cultureName:="", publicKeyOrToken:=publicKeyB, hasPublicKey:=True),
                {sourceB},
                references:={MscorlibRef, referenceAS1},
                options:=TestOptions.DebugDll.WithDelaySign(True))
            Dim referenceBS1 = compilationBS1.EmitToImageReference()
            Dim identityBS1 = referenceBS1.GetAssemblyIdentity()
            Dim compilationBS2 = CreateCompilation(
                New AssemblyIdentity(assemblyNameB, New Version(2, 2, 2, 1), cultureName:="", publicKeyOrToken:=publicKeyB, hasPublicKey:=True),
                {sourceB},
                references:={MscorlibRef, referenceAS2},
                options:=TestOptions.DebugDll.WithDelaySign(True))
            Dim referenceBS2 = compilationBS2.EmitToImageReference()
            Dim identityBS2 = referenceBS2.GetAssemblyIdentity()

            ' Assembly C, multiple versions, not strong name.
            Dim assemblyNameC = ExpressionCompilerUtilities.GenerateUniqueName()
            Dim compilationCN1 = CreateCompilation(
                New AssemblyIdentity(assemblyNameC, New Version(1, 1, 1, 1)),
                {sourceC},
                references:={MscorlibRef, referenceBS1},
                options:=TestOptions.DebugDll)
            Dim exeBytesC1 As Byte() = Nothing
            Dim pdbBytesC1 As Byte() = Nothing
            Dim references As ImmutableArray(Of MetadataReference) = Nothing
            compilationCN1.EmitAndGetReferences(exeBytesC1, pdbBytesC1, references)
            Dim compilationCN2 = CreateCompilation(
                New AssemblyIdentity(assemblyNameC, New Version(2, 1, 1, 1)),
                {sourceC},
                references:={MscorlibRef, referenceBS2},
                options:=TestOptions.DebugDll)
            Dim exeBytesC2 As Byte() = Nothing
            Dim pdbBytesC2 As Byte() = Nothing
            compilationCN1.EmitAndGetReferences(exeBytesC2, pdbBytesC2, references)

            ' Duplicate assemblies, target module referencing BS1.
            Using runtime = CreateRuntimeInstance(
                assemblyNameC,
                ImmutableArray.Create(MscorlibRef, referenceAS1, referenceAS2, referenceBS2, referenceBS1, referenceBS2),
                exeBytesC1,
                New SymReader(pdbBytesC1))

                Dim typeBlocks As ImmutableArray(Of MetadataBlock) = Nothing
                Dim methodBlocks As ImmutableArray(Of MetadataBlock) = Nothing
                Dim moduleVersionId As Guid = Nothing
                Dim symReader As ISymUnmanagedReader = Nothing
                Dim typeToken = 0
                Dim methodToken = 0
                Dim localSignatureToken = 0
                GetContextState(runtime, "C", typeBlocks, moduleVersionId, symReader, typeToken, localSignatureToken)
                GetContextState(runtime, "C.M", methodBlocks, moduleVersionId, symReader, methodToken, localSignatureToken)

                ' Compile expression with type context.
                Dim context = EvaluationContext.CreateTypeContext(
                    Nothing,
                    typeBlocks,
                    moduleVersionId,
                    typeToken)
                Dim errorMessage As String = Nothing
                ' A is ambiguous since there were no explicit references to AS1 or AS2.
                Dim testData = New CompilationTestData()
                context.CompileExpression("New A()", errorMessage, testData)
                Assert.Equal(errorMessage, "(1,6): error BC30554: 'A' is ambiguous.")
                testData = New CompilationTestData()
                ' Ideally, B should be resolved to BS1.
                context.CompileExpression("New B()", errorMessage, testData)
                Assert.Equal(errorMessage, "(1,6): error BC30554: 'B' is ambiguous.")

                ' Compile expression with method context.
                Dim previous = New VisualBasicMetadataContext(typeBlocks, context)
                context = EvaluationContext.CreateMethodContext(
                    previous,
                    methodBlocks,
                    MakeDummyLazyAssemblyReaders(),
                    symReader,
                    moduleVersionId,
                    methodToken,
                    methodVersion:=1,
                    ilOffset:=0,
                    localSignatureToken:=localSignatureToken)
                Assert.Equal(previous.Compilation, context.Compilation) ' re-use type context compilation
                testData = New CompilationTestData()
                ' Ideally, B should be resolved to BS1.
                context.CompileExpression("New B()", errorMessage, testData)
                Assert.Equal(errorMessage, "(1,6): error BC30554: 'B' is ambiguous.")
            End Using
        End Sub

        <Fact>
        Public Sub DuplicateTypesAndMethodsDifferentAssemblies()
            Const sourceA =
"Option Strict On
Imports System.Runtime.CompilerServices
Imports N
Namespace N
    Class C1
    End Class
    Public Module E
        <Extension>
        Public Function F(o As A) As Integer
            Return 1
        End Function
    End Module
End Namespace
Class C2
End Class
Public Class A
    Public Shared Sub M()
        Dim x As New A()
        Dim y As Object = x.F()
    End Sub
End Class"
            Const sourceB =
"Option Strict On
Imports System.Runtime.CompilerServices
Imports N
Namespace N
    Class C1
    End Class
    Public Module E
        <Extension>
        Public Function F(o As A) As Integer
            Return 2
        End Function
    End Module
End Namespace
Class C2
End Class
Class B
    Shared Sub Main()
        Dim x As New A()
    End Sub
End Class"
            Dim assemblyNameA = ExpressionCompilerUtilities.GenerateUniqueName()
            Dim compilationA = CreateCompilationWithMscorlibAndVBRuntime(
                MakeSources(sourceA, assemblyName:=assemblyNameA),
                options:=TestOptions.DebugDll,
                additionalRefs:={SystemCoreRef})
            Dim exeBytesA As Byte() = Nothing
            Dim pdbBytesA As Byte() = Nothing
            Dim referencesA As ImmutableArray(Of MetadataReference) = Nothing
            compilationA.EmitAndGetReferences(exeBytesA, pdbBytesA, referencesA)
            Dim referenceA = AssemblyMetadata.CreateFromImage(exeBytesA).GetReference(display:=assemblyNameA)
            Dim moduleA = referenceA.ToModuleInstance(exeBytesA, New SymReader(pdbBytesA))

            Dim assemblyNameB = ExpressionCompilerUtilities.GenerateUniqueName()
            Dim compilationB = CreateCompilationWithMscorlibAndVBRuntime(
                MakeSources(sourceB, assemblyName:=assemblyNameB),
                options:=TestOptions.DebugDll,
                additionalRefs:={SystemCoreRef, referenceA})
            Dim exeBytesB As Byte() = Nothing
            Dim pdbBytesB As Byte() = Nothing
            Dim referencesB As ImmutableArray(Of MetadataReference) = Nothing
            compilationB.EmitAndGetReferences(exeBytesB, pdbBytesB, referencesB)
            Dim referenceB = AssemblyMetadata.CreateFromImage(exeBytesB).GetReference(display:=assemblyNameB)
            Dim moduleB = referenceB.ToModuleInstance(exeBytesB, New SymReader(pdbBytesB))

            Dim moduleBuilder = ArrayBuilder(Of ModuleInstance).GetInstance()
            moduleBuilder.AddRange(referencesA.Select(Function(r) r.ToModuleInstance(Nothing, Nothing)))
            moduleBuilder.Add(moduleA)
            moduleBuilder.Add(moduleB)
            Dim modules = moduleBuilder.ToImmutableAndFree()

            Using runtime = New RuntimeInstance(modules)
                Dim blocks As ImmutableArray(Of MetadataBlock) = Nothing
                Dim moduleVersionId As Guid = Nothing
                Dim symReader As ISymUnmanagedReader = Nothing
                Dim typeToken = 0
                Dim methodToken = 0
                Dim localSignatureToken = 0
                GetContextState(runtime, "B", blocks, moduleVersionId, symReader, typeToken, localSignatureToken)
                Dim contextFactory = CreateTypeContextFactory(moduleVersionId, typeToken)

                ' Duplicate type in namespace, at type scope.
                Dim testData As CompilationTestData = Nothing
                Dim errorMessage As String = Nothing
                ExpressionCompilerTestHelpers.CompileExpressionWithRetry(blocks, "New N.C1()", contextFactory, errorMessage, testData)
                Assert.Equal(errorMessage, "(1,6): error BC30560: 'C1' is ambiguous in the namespace 'N'.")

                GetContextState(runtime, "B.Main", blocks, moduleVersionId, symReader, methodToken, localSignatureToken)
                contextFactory = CreateMethodContextFactory(moduleVersionId, symReader, methodToken, localSignatureToken)

                ' Duplicate type in namespace, at method scope.
                ExpressionCompilerTestHelpers.CompileExpressionWithRetry(blocks, "New C1()", contextFactory, errorMessage, testData)
                Assert.Equal(errorMessage, "(1,6): error BC30560: 'C1' is ambiguous in the namespace 'N'.")

                ' Duplicate type in global namespace, at method scope.
                ExpressionCompilerTestHelpers.CompileExpressionWithRetry(blocks, "New C2()", contextFactory, errorMessage, testData)
                Assert.Equal(errorMessage, "(1,6): error BC30554: 'C2' is ambiguous.")

                ' Duplicate extension method, at method scope.
                ExpressionCompilerTestHelpers.CompileExpressionWithRetry(blocks, "x.F()", contextFactory, errorMessage, testData)
                Assert.Equal(errorMessage, "(1,4): error BC30521: Overload resolution failed because no accessible 'F' is most specific for these arguments:
    Extension method 'Public Function F() As Integer' defined in 'E': Not most specific.
    Extension method 'Public Function F() As Integer' defined in 'E': Not most specific.")

                ' Same tests as above but in library that does not directly reference duplicates.
                GetContextState(runtime, "A", blocks, moduleVersionId, symReader, typeToken, localSignatureToken)
                contextFactory = CreateTypeContextFactory(moduleVersionId, typeToken)

                ' Duplicate type in namespace, at type scope.
                ExpressionCompilerTestHelpers.CompileExpressionWithRetry(blocks, "New N.C1()", contextFactory, errorMessage, testData)
                Assert.Null(errorMessage)
                testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  newobj     ""Sub N.C1..ctor()""
  IL_0005:  ret
}")

                GetContextState(runtime, "A.M", blocks, moduleVersionId, symReader, methodToken, localSignatureToken)
                contextFactory = CreateMethodContextFactory(moduleVersionId, symReader, methodToken, localSignatureToken)

                ' Duplicate type in global namespace, at method scope.
                ExpressionCompilerTestHelpers.CompileExpressionWithRetry(blocks, "New C2()", contextFactory, errorMessage, testData)
                Assert.Null(errorMessage)
                testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (A V_0, //x
                Object V_1) //y
  IL_0000:  newobj     ""Sub C2..ctor()""
  IL_0005:  ret
}")

                ' Duplicate extension method, at method scope.
                ExpressionCompilerTestHelpers.CompileExpressionWithRetry(blocks, "x.F()", contextFactory, errorMessage, testData)
                Assert.Null(errorMessage)
                testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (A V_0, //x
                Object V_1) //y
  IL_0000:  ldloc.0
  IL_0001:  call       ""Function N.E.F(A) As Integer""
  IL_0006:  ret
}")
            End Using
        End Sub

        Private Shared Function CreateTypeContextFactory(
            moduleVersionId As Guid,
            typeToken As Integer) As ExpressionCompiler.CreateContextDelegate

            Return Function(blocks, useReferencedModulesOnly)
                       Dim compilation = If(useReferencedModulesOnly, blocks.ToCompilationReferencedModulesOnly(moduleVersionId), blocks.ToCompilation())
                       Return EvaluationContext.CreateTypeContext(
                           compilation,
                           moduleVersionId,
                           typeToken)
                   End Function
        End Function

        Private Shared Function CreateMethodContextFactory(
            moduleVersionId As Guid,
            symReader As ISymUnmanagedReader,
            methodToken As Integer,
            localSignatureToken As Integer) As ExpressionCompiler.CreateContextDelegate

            Return Function(blocks, useReferencedModulesOnly)
                       Dim compilation = If(useReferencedModulesOnly, blocks.ToCompilationReferencedModulesOnly(moduleVersionId), blocks.ToCompilation())
                       Return EvaluationContext.CreateMethodContext(
                            compilation,
                            MakeDummyLazyAssemblyReaders(),
                            symReader,
                            moduleVersionId,
                            methodToken,
                            methodVersion:=1,
                            ilOffset:=0,
                            localSignatureToken:=localSignatureToken)
                   End Function
        End Function

    End Class

End Namespace
