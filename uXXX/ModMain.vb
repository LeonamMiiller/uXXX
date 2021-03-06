﻿Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Module ModMain

    Private Enum File_Type
        Package '-1
        ObjectReferencer '-2
        SoundNodeWave '-3
    End Enum

    Const TAB As String = "    "
    Const TAB_2 As String = TAB & TAB
    Const TAB_3 As String = TAB_2 & TAB

    Sub Main()
        Console.Title = "uXXX Beta"
        Console.ForegroundColor = ConsoleColor.Cyan
        Console.WriteLine("uXXX v0.2 by gdkchan")
        Dim Args() As String = Environment.GetCommandLineArgs
        Dim Remaining_Args As Integer = Args.Count - 1
        Dim CurrArg As Integer = 1
        Do
            Dim File_Name As String
            If Args.Count < 2 Then
                Console.ForegroundColor = ConsoleColor.White
                Console.WriteLine("Digite o nome do arquivo:")
                Console.ForegroundColor = ConsoleColor.Gray
                File_Name = Console.ReadLine
            Else
                File_Name = Args(CurrArg)
            End If
            Console.ForegroundColor = ConsoleColor.White
            Console.WriteLine("------")

            If File.Exists(File_Name) Then
                Dim Data() As Byte = File.ReadAllBytes(File_Name)

                Dim Magic As Integer = Read32(Data, 0)
                If Magic = &H9E2A83C1 Then
                    Dim Header_XML As New StringBuilder
                    Dim Text_Out As New StringBuilder
                    Header_XML.AppendLine("<Header>")

                    Dim Licensee_Version As Integer = Read16(Data, 4)
                    Dim Version As Integer = Read16(Data, 6)
                    Dim Header_Length As Integer = Read32(Data, 8)

                    Header_XML.AppendLine(TAB & "<Version>" & Version & "</Version>")
                    Header_XML.AppendLine(TAB & "<LicenseVer>" & Licensee_Version & "</LicenseVer>")

                    Dim Folder_Name_Length As Integer = Read32(Data, 12)
                    Dim Folder_Name As String = ReadStr(Data, 16, Folder_Name_Length - 1)
                    Dim Work_Dir As String = Path.Combine(Path.GetDirectoryName(File_Name), Path.GetFileNameWithoutExtension(File_Name))
                    Directory.CreateDirectory(Work_Dir)
                    Dim Base_Offset As Integer = 16 + Folder_Name_Length
                    Dim Flags As Integer = Read32(Data, Base_Offset)

                    Header_XML.AppendLine(TAB & "<FolderName>" & Folder_Name & "</FolderName>")
                    Header_XML.AppendLine(TAB & "<Flags>" & "0x" & Hex(Flags).PadLeft(8, "0"c) & "</Flags>")

                    Dim Names_Count As Integer = Read32(Data, Base_Offset + 4)
                    Dim Names_Offset As Integer = Read32(Data, Base_Offset + 8)
                    Dim Export_Table_Count As Integer = Read32(Data, Base_Offset + 12)
                    Dim Export_Table_Offset As Integer = Read32(Data, Base_Offset + 16)
                    Dim Import_Table_Count As Integer = Read32(Data, Base_Offset + 20)
                    Dim Import_Table_Offset As Integer = Read32(Data, Base_Offset + 24)
                    Dim Zero_Pad_Length As Integer
                    If Read32(Data, Base_Offset + 32) > 0 Then Zero_Pad_Length = Read32(Data, Base_Offset + 32) - Read32(Data, Base_Offset + 28)

                    Dim GUID As String = "0x"
                    For Offset As Integer = Base_Offset + 48 To Base_Offset + 48 + 15
                        GUID &= Hex(Data(Offset)).PadLeft(2, "0"c)
                    Next
                    Header_XML.AppendLine(TAB & "<GUID>" & GUID & "</GUID>")

                    Dim Generations_Count As Integer = Read32(Data, Base_Offset + 64)
                    Dim Net_Object_Count As Integer = If(Generations_Count > 0, Read32(Data, Base_Offset + 76), 0)
                    Dim Engine_Version As Integer = Read32(Data, Base_Offset + (Generations_Count * 12) + 68)
                    Dim Cooker_Version As Integer = Read32(Data, Base_Offset + (Generations_Count * 12) + 72)
                    Dim Data_2 As Integer = Read32(Data, Base_Offset + (Generations_Count * 12) + 84)

                    Header_XML.AppendLine(TAB & "<EngineVersion>" & Engine_Version & "</EngineVersion>")
                    Header_XML.AppendLine(TAB & "<CookerVersion>" & Cooker_Version & "</CookerVersion>")
                    Header_XML.AppendLine(TAB & "<GenerationsCount>" & Generations_Count & "</GenerationsCount>")
                    Header_XML.AppendLine(TAB & "<NetObjCount>" & Net_Object_Count & "</NetObjCount>")
                    Header_XML.AppendLine(TAB & "<ExFlags>" & "0x" & Hex(Data_2).PadLeft(8, "0"c) & "</ExFlags>") 'Não sei o que é D:
                    Header_XML.AppendLine(TAB & "<ZeroPadLength>" & Zero_Pad_Length & "</ZeroPadLength>")

                    'Name Table
                    Header_XML.AppendLine(TAB & "<Names>")
                    For Index As Integer = 0 To Names_Count - 1
                        Header_XML.AppendLine(TAB_2 & "<Entry>")
                        Header_XML.AppendLine(TAB_3 & "<Text>" & ReadName(Data, Names_Offset, Names_Count, Index) & "</Text>")
                        Header_XML.AppendLine(TAB_3 & "<Flags>" & "0x" & Hex(ReadNameFlags(Data, Names_Offset, Names_Count, Index)).PadLeft(16, "0"c) & "</Flags>")
                        Header_XML.AppendLine(TAB_2 & "</Entry>")
                    Next
                    Header_XML.AppendLine(TAB & "</Names>")

                    'Export Table
                    Header_XML.AppendLine(TAB & "<Export>")

                    Dim EOffset As Integer = Export_Table_Offset
                    For Entry As Integer = 0 To Export_Table_Count - 1
                        Dim ObjTypeRef As Integer = Read32(Data, EOffset)
                        Dim ParentClassRef As Integer = Read32(Data, EOffset + 4)
                        Dim Length As Integer = Read32(Data, EOffset + 44)
                        Dim Entry_Length As Integer = &H44 + (Length * 4)

                        Dim OwnerRef As Integer = Read32(Data, EOffset + 8)
                        Dim Name_Index As Integer = Read32(Data, EOffset + 12)
                        Dim Object_Reference As Integer = Read32(Data, EOffset + 16) - 1
                        Dim Name_Count As Integer = Read32(Data, EOffset + 20) - 1
                        Dim Object_Name As String = ReadName(Data, Names_Offset, Names_Count, Name_Index)
                        Dim Name As String = Object_Name
                        If Object_Reference > -1 Then Name &= "_" & Object_Reference
                        If Name_Count > -1 Then Name &= "_" & Name_Count
                        Name &= "_" & Entry
                        Dim Flags_1 As UInt64 = Read64(Data, EOffset + 24)
                        Dim File_Length As Integer = Read32(Data, EOffset + 32)
                        Dim File_Offset As Integer = Read32(Data, EOffset + 36)
                        Dim Exporter_Flags As Integer = Read32(Data, EOffset + 40)
                        '32 bytes que eu não sei o que é (pelo menos no Package)

                        Try 'Dumpa o texto
                            If ObjTypeRef = -3 Then 'É SoundNodeWave, então podemos tentar extrair legendas
                                Dim Text_Count As Integer = Read32(Data, File_Offset + &H8C)
                                Dim Sub_Offset As Integer = &HA8
                                For Index As Integer = 0 To Text_Count - 1
                                    Dim Len As Integer = Read32(Data, File_Offset + Sub_Offset)
                                    Sub_Offset += Len + &H40
                                Next
                                Sub_Offset += &H20

                                If ReadStr(Data, File_Offset + Sub_Offset, 3) = "INT" Then 'Magic, texto em inglês
                                    Sub_Offset += &H38
                                    Dim Subtitle As String = Nothing
                                    For Index As Integer = 0 To Text_Count - 1
                                        Dim Text_Length As Integer = Read32(Data, File_Offset + Sub_Offset)
                                        Subtitle &= ReadStr(Data, File_Offset + Sub_Offset + 4, Text_Length - 1)
                                        If Index < Text_Count - 1 Then Subtitle &= "\n"
                                        Sub_Offset += Text_Length + &H40
                                    Next
                                    Text_Out.AppendLine("[" & Name & "]")
                                    Text_Out.AppendLine(Subtitle)
                                    Text_Out.AppendLine("[END]")
                                End If
                            End If

                            If Object_Name = "DisConv_Blurb" Then
                                Dim Sub_Offset As Integer = If(Read32(Data, File_Offset + &H76) And 4, &H9A, &HB3)
                                Dim Text_Length As Integer = Read32(Data, File_Offset + Sub_Offset)
                                Dim Subtitle As String = ReadStr(Data, File_Offset + Sub_Offset + 4, Text_Length - 1)

                                Text_Out.AppendLine("[" & Name & "]")
                                Text_Out.AppendLine(Subtitle)
                                Text_Out.AppendLine("[END]")
                            End If
                        Catch
                            Debug.WriteLine("Erro ao extrair texto :/")
                        End Try

                        Header_XML.AppendLine(TAB_2 & "<Entry>")
                        Header_XML.AppendLine(TAB_3 & "<File>" & Name & "</File>")
                        Header_XML.AppendLine(TAB_3 & "<NameIndex>" & Name_Index & "</NameIndex>" & " <!--" & ReadName(Data, Names_Offset, Names_Count, Name_Index) & "-->")
                        Header_XML.AppendLine(TAB_3 & "<NameCount>" & Name_Count & "</NameCount>")
                        Header_XML.AppendLine(TAB_3 & "<Class>" & ObjTypeRef & "</Class>")
                        Header_XML.AppendLine(TAB_3 & "<ParentClassRef>" & ParentClassRef & "</ParentClassRef>")
                        Header_XML.AppendLine(TAB_3 & "<ObjReference>" & Object_Reference & "</ObjReference>")
                        Header_XML.AppendLine(TAB_3 & "<OwnerRef>" & OwnerRef & "</OwnerRef>")
                        Header_XML.AppendLine(TAB_3 & "<Flags>" & "0x" & Hex(Flags_1).PadLeft(16, "0"c) & "</Flags>")
                        Header_XML.AppendLine(TAB_3 & "<ExporterFlags>" & "0x" & Hex(Exporter_Flags).PadLeft(8, "0"c) & "</ExporterFlags>")
                        Header_XML.AppendLine(TAB_3 & "<EntryLength>" & Length & "</EntryLength>")
                        Dim ExData As String = "0x"
                        For Offset As Integer = EOffset + 48 To EOffset + Entry_Length - 1
                            ExData &= Hex(Data(Offset)).PadLeft(2, "0"c)
                        Next
                        Header_XML.AppendLine(TAB_3 & "<ExData>" & ExData & "</ExData>")
                        Header_XML.AppendLine(TAB_2 & "</Entry>")

                        Dim Out_Data(File_Length - 1) As Byte
                        Buffer.BlockCopy(Data, File_Offset, Out_Data, 0, File_Length)
                        Dim Out_File As String = Path.Combine(Work_Dir, Name)
                        Console.WriteLine("Extraindo " & Name & "...")
                        File.WriteAllBytes(Out_File, Out_Data)

                        EOffset += Entry_Length
                    Next

                    Header_XML.AppendLine(TAB & "</Export>")

                    Header_XML.AppendLine(TAB & "<Import>")

                    Dim IOffset As Integer = Import_Table_Offset
                    For Entry As Integer = 0 To Import_Table_Count - 1
                        Dim Package_Index As Integer = Read32(Data, IOffset)
                        Dim Class_Index As Integer = Read32(Data, IOffset + 8)
                        Dim Outer As Integer = Read32(Data, IOffset + 16)
                        Dim Object_Index As Integer = Read32(Data, IOffset + 20)
                        Dim Unknow_Index As Integer = Read32(Data, IOffset + 24)

                        Header_XML.AppendLine(TAB_2 & "<Entry>")
                        Header_XML.AppendLine(TAB_3 & "<PackageNameIndex>" & Package_Index & "</PackageNameIndex>" & " <!--" & ReadName(Data, Names_Offset, Names_Count, Package_Index) & "-->")
                        Header_XML.AppendLine(TAB_3 & "<ClassNameIndex>" & Class_Index & "</ClassNameIndex>" & " <!--" & ReadName(Data, Names_Offset, Names_Count, Class_Index) & "-->")
                        Header_XML.AppendLine(TAB_3 & "<ObjectNameIndex>" & Object_Index & "</ObjectNameIndex>" & " <!--" & ReadName(Data, Names_Offset, Names_Count, Object_Index) & "-->")
                        Header_XML.AppendLine(TAB_3 & "<Outer>" & Outer & "</Outer>")
                        Header_XML.AppendLine(TAB_3 & "<ExIndex>" & Unknow_Index & "</ExIndex>")
                        Header_XML.AppendLine(TAB_2 & "</Entry>")

                        IOffset += 28
                    Next

                    Header_XML.AppendLine(TAB & "</Import>")

                    Header_XML.AppendLine("</Header>")

                    Console.WriteLine("Salvando ""Header.xml""...")
                    File.WriteAllText(Path.Combine(Work_Dir, "Header.xml"), Header_XML.ToString)

                    If Text_Out.Length > 0 Then
                        Console.WriteLine("Salvando ""Textos.txt""...")
                        File.WriteAllText(Path.Combine(Work_Dir, "Textos.txt"), Text_Out.ToString)
                    End If

                    If Base_Offset + (Generations_Count * 12) + 92 < Names_Offset Then 'Mais dados desconhecidos D:
                        Dim Unknow_Offset As Integer = Base_Offset + (Generations_Count * 12) + 88
                        Dim Unknow_Length As Integer = Names_Offset - Unknow_Offset
                        Dim Unknow(Unknow_Length - 1) As Byte
                        Buffer.BlockCopy(Data, Unknow_Offset, Unknow, 0, Unknow_Length)

                        Console.WriteLine("Salvando ""ExHeader.bin""...")
                        File.WriteAllBytes(Path.Combine(Work_Dir, "ExHeader.bin"), Unknow)
                    End If
                Else
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine("Esse arquivo não é XXX!")
                    Console.ReadKey()
                    Console.ResetColor()
                    End
                End If
            ElseIf Directory.Exists(File_Name) Then
                If Not File.Exists(Path.Combine(File_Name, "Header.xml")) Then
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine("Arquivo ""Header.xml"" não encontrado!")
                    Console.ReadKey()
                    Console.ResetColor()
                    End
                End If
                Dim Header_XML As String = File.ReadAllText(Path.Combine(File_Name, "Header.xml"))
                Dim Header_Section As String = Regex.Match(Header_XML, "<Header>(.+?)</Header>", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Groups(1).Value
                Dim Data As New MemoryStream()

                Write32(Data, 0, &H9E2A83C1) 'Magic

                Dim Licensee_Version As Integer = Integer.Parse(Regex.Match(Header_Section, "<LicenseVer>(\d+)</LicenseVer>", RegexOptions.IgnoreCase).Groups(1).Value)
                Dim Version As Integer = Integer.Parse(Regex.Match(Header_Section, "<Version>(\d+)</Version>", RegexOptions.IgnoreCase).Groups(1).Value)

                Write16(Data, 4, Licensee_Version)
                Write16(Data, 6, Version)

                Dim Folder_Name As String = Regex.Match(Header_Section, "<FolderName>(.+?)</FolderName>", RegexOptions.IgnoreCase).Groups(1).Value
                Dim Bytes() As Byte = Encoding.ASCII.GetBytes(Folder_Name)
                Write32(Data, 12, Bytes.Length + 1)
                Data.Write(Bytes, 0, Bytes.Length)
                Data.WriteByte(0)
                Dim Base_Offset As Integer = Data.Position
                Dim Flags As Integer = Convert.ToInt32(Regex.Match(Header_Section, "<Flags>0x([0-9A-Fa-f]+)</Flags>", RegexOptions.IgnoreCase).Groups(1).Value, 16)
                Write32(Data, Base_Offset, Flags)
                Data.Seek(Base_Offset + 48, SeekOrigin.Begin)
                Dim GUID As String = Regex.Match(Header_Section, "<GUID>0x([0-9A-Fa-f]+)</GUID>", RegexOptions.IgnoreCase).Groups(1).Value
                For Position As Integer = 0 To GUID.Length - 1 Step 2
                    Dim Hex_Value As String = GUID.Substring(Position, 2)
                    Dim Value As Byte = Convert.ToByte(Convert.ToInt32(Hex_Value, 16) And &HFF)
                    Data.WriteByte(Value)
                Next
                Dim Generations_Count As Integer = Integer.Parse(Regex.Match(Header_Section, "<GenerationsCount>(\d+)</GenerationsCount>", RegexOptions.IgnoreCase).Groups(1).Value)
                Dim Net_Object_Count As Integer = Integer.Parse(Regex.Match(Header_Section, "<NetObjCount>(\d+)</NetObjCount>", RegexOptions.IgnoreCase).Groups(1).Value)
                Dim Engine_Version As Integer = Integer.Parse(Regex.Match(Header_Section, "<EngineVersion>(\d+)</EngineVersion>", RegexOptions.IgnoreCase).Groups(1).Value)
                Dim Cooker_Version As Integer = Integer.Parse(Regex.Match(Header_Section, "<CookerVersion>(\d+)</CookerVersion>", RegexOptions.IgnoreCase).Groups(1).Value)
                Dim ExFlags As Integer = Convert.ToInt32(Regex.Match(Header_Section, "<ExFlags>0x([0-9A-Fa-f]+)</ExFlags>", RegexOptions.IgnoreCase).Groups(1).Value, 16)

                Write32(Data, Base_Offset + 64, Generations_Count)
                Write32(Data, Base_Offset + 76, Net_Object_Count)
                Write32(Data, Base_Offset + (Generations_Count * 12) + 68, Engine_Version)
                Write32(Data, Base_Offset + (Generations_Count * 12) + 72, Cooker_Version)
                Write32(Data, Base_Offset + (Generations_Count * 12) + 84, ExFlags)

                Dim Names_Offset As Integer = Base_Offset + (Generations_Count * 12) + 92
                If File.Exists(Path.Combine(File_Name, "ExHeader.bin")) Then
                    Dim Unknow() As Byte = File.ReadAllBytes(Path.Combine(File_Name, "ExHeader.bin"))
                    Data.Write(Unknow, 0, Unknow.Length)
                    Names_Offset += Unknow.Length - 4
                End If

                Dim Names_Section As String = Regex.Match(Header_Section, "<Names>(.+?)</Names>", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Groups(1).Value
                Dim Names_Entries As MatchCollection = Regex.Matches(Names_Section, "<Entry>(.+?)</Entry>", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                Dim Name_Table(Names_Entries.Count - 1) As String
                Write32(Data, Base_Offset + 4, Names_Entries.Count)
                If Generations_Count > 0 Then Write32(Data, Base_Offset + 72, Names_Entries.Count)
                Write32(Data, Base_Offset + 8, Names_Offset)
                Dim Name_Entry As Integer = 0
                For Each Entry As Match In Names_Entries
                    Dim Content As String = Entry.Groups(1).Value
                    Dim Text As String = Regex.Match(Content, "<Text>(.+?)</Text>", RegexOptions.IgnoreCase).Groups(1).Value
                    Dim Text_Flags As UInt64 = Convert.ToUInt64(Regex.Match(Content, "<Flags>0x([0-9A-Fa-f]+)</Flags>", RegexOptions.IgnoreCase).Groups(1).Value, 16)
                    Name_Table(Name_Entry) = Text

                    Dim Text_Bytes() As Byte = Get_Bytes_From_Text(Text)
                    Write32(Data, Names_Offset, Text_Bytes.Length + 1)
                    Data.Write(Text_Bytes, 0, Text_Bytes.Count)
                    Data.WriteByte(0)
                    Names_Offset = Data.Position
                    Write64(Data, Names_Offset, Text_Flags)
                    Names_Offset += 8
                    Name_Entry += 1
                Next

                Dim Import_Offset As Integer = Names_Offset
                Dim Import_Section As String = Regex.Match(Header_Section, "<Import>(.+?)</Import>", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Groups(1).Value
                Dim Import_Entries As MatchCollection = Regex.Matches(Import_Section, "<Entry>(.+?)</Entry>", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                Write32(Data, Base_Offset + 20, Import_Entries.Count)
                Write32(Data, Base_Offset + 24, Import_Offset)
                For Each Entry As Match In Import_Entries
                    Dim Content As String = Entry.Groups(1).Value

                    Dim Package_Index As String = Integer.Parse(Regex.Match(Content, "<PackageNameIndex>(\d+)</PackageNameIndex>", RegexOptions.IgnoreCase).Groups(1).Value)
                    Dim Class_Index As String = Integer.Parse(Regex.Match(Content, "<ClassNameIndex>(\d+)</ClassNameIndex>", RegexOptions.IgnoreCase).Groups(1).Value)
                    Dim Outer As Integer = Integer.Parse(Regex.Match(Content, "<Outer>([-]?\d+)</Outer>", RegexOptions.IgnoreCase).Groups(1).Value)
                    Dim Object_Index As String = Integer.Parse(Regex.Match(Content, "<ObjectNameIndex>(\d+)</ObjectNameIndex>", RegexOptions.IgnoreCase).Groups(1).Value)
                    Dim Unknow_Index As String = Integer.Parse(Regex.Match(Content, "<ExIndex>(\d+)</ExIndex>", RegexOptions.IgnoreCase).Groups(1).Value)

                    Write32(Data, Import_Offset, Package_Index)
                    Write32(Data, Import_Offset + 8, Class_Index)
                    Write32(Data, Import_Offset + 16, Outer)
                    Write32(Data, Import_Offset + 20, Object_Index)
                    Write32(Data, Import_Offset + 24, Unknow_Index)

                    Import_Offset += 28
                Next

                Dim Export_Offset As Integer = Import_Offset
                Dim Export_Section As String = Regex.Match(Header_Section, "<Export>(.+?)</Export>", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Groups(1).Value
                Dim Export_Entries As MatchCollection = Regex.Matches(Export_Section, "<Entry>(.+?)</Entry>", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                Write32(Data, Base_Offset + 12, Export_Entries.Count)
                If Generations_Count > 0 Then Write32(Data, Base_Offset + 68, Export_Entries.Count)
                Write32(Data, Base_Offset + 16, Export_Offset)

                Dim Export_Length As Integer = 0
                For Each Entry As Match In Export_Entries
                    Dim Content As String = Entry.Groups(1).Value

                    Dim Length As Integer = Integer.Parse(Regex.Match(Content, "<EntryLength>([-]?\d+)</EntryLength>", RegexOptions.IgnoreCase).Groups(1).Value)
                    Export_Length += &H44 + (Length * 4)
                Next
                Dim File_Offset As Integer = Export_Offset + Export_Length
                Write32(Data, Base_Offset + 28, File_Offset)
                File_Offset += Integer.Parse(Regex.Match(Header_Section, "<ZeroPadLength>(\d+)</ZeroPadLength>", RegexOptions.IgnoreCase).Groups(1).Value)
                Write32(Data, 8, File_Offset)
                Write32(Data, Base_Offset + 32, File_Offset)

                For Each Entry As Match In Export_Entries
                    Dim Content As String = Entry.Groups(1).Value

                    Dim Temp_File As String = Regex.Match(Content, "<File>(.+?)</File>", RegexOptions.IgnoreCase).Groups(1).Value
                    If Not File.Exists(Path.Combine(File_Name, Temp_File)) Then
                        Console.ForegroundColor = ConsoleColor.Red
                        Console.WriteLine("Arquivo """ & Temp_File & """ não encontrado!")
                        Console.ReadKey()
                        Console.ResetColor()
                        End
                    End If
                    Dim File_Data() As Byte = File.ReadAllBytes(Path.Combine(File_Name, Temp_File))
                    Dim ObjTypeRef As Integer = Integer.Parse(Regex.Match(Content, "<Class>([-]?\d+)</Class>", RegexOptions.IgnoreCase).Groups(1).Value)
                    Dim ParentClassRef As Integer = Integer.Parse(Regex.Match(Content, "<ParentClassRef>([-]?\d+)</ParentClassRef>", RegexOptions.IgnoreCase).Groups(1).Value)
                    Dim Name_Index As Integer = Integer.Parse(Regex.Match(Content, "<NameIndex>(\d+)</NameIndex>", RegexOptions.IgnoreCase).Groups(1).Value)
                    Dim Object_Reference As Integer = Integer.Parse(Regex.Match(Content, "<ObjReference>([-]?\d+)</ObjReference>", RegexOptions.IgnoreCase).Groups(1).Value) + 1
                    Dim Name_Count As Integer = Integer.Parse(Regex.Match(Content, "<NameCount>([-]?\d+)</NameCount>", RegexOptions.IgnoreCase).Groups(1).Value) + 1
                    Dim OwnerRef As Integer = Integer.Parse(Regex.Match(Content, "<OwnerRef>([-]?\d+)</OwnerRef>", RegexOptions.IgnoreCase).Groups(1).Value)
                    Dim Flags_1 As UInt64 = Convert.ToUInt64(Regex.Match(Content, "<Flags>0x([0-9A-Fa-f]+)</Flags>", RegexOptions.IgnoreCase).Groups(1).Value, 16)
                    Dim Exporter_Flags As Integer = Convert.ToInt32(Regex.Match(Content, "<ExporterFlags>0x([0-9A-Fa-f]+)</ExporterFlags>", RegexOptions.IgnoreCase).Groups(1).Value, 16)
                    Dim Length As Integer = Integer.Parse(Regex.Match(Content, "<EntryLength>([-]?\d+)</EntryLength>", RegexOptions.IgnoreCase).Groups(1).Value)

                    If File.Exists(Path.Combine(File_Name, "Textos.txt")) Then 'Re-insere o texto
                        Dim TempTxtData() As Byte = File.ReadAllBytes(Path.Combine(File_Name, "Textos.txt"))
                        Dim SubText As String = Encoding.UTF8.GetString(TempTxtData)
                        Dim Match As Match = Regex.Match(SubText, "\[" & Temp_File & "\]\r\n(.+?)\r\n\[END\]")
                        If Match.Success Then
                            If ObjTypeRef = -3 Then
                                Dim Text_Count As Integer = Read32(File_Data, &H8C)
                                Dim Sub_Offset As Integer = &HA8
                                For Index As Integer = 0 To Text_Count - 1
                                    Dim Len As Integer = Read32(File_Data, Sub_Offset)
                                    Sub_Offset += Len + &H40
                                Next
                                Sub_Offset += &H20
                                If Sub_Offset < File_Data.Length Then
                                    If ReadStr(File_Data, Sub_Offset, 3) = "INT" Then 'Magic, texto em inglês
                                        Dim Temp As New MemoryStream()
                                        Sub_Offset += &H38
                                        Dim Subs(0) As String
                                        If Match.Groups(1).Value.IndexOf("\n") > -1 Then
                                            Subs = Regex.Split(Match.Groups(1).Value, "\\n")
                                        Else
                                            Subs(0) = Match.Groups(1).Value
                                        End If

                                        Temp.Write(File_Data, 0, Sub_Offset)
                                        Dim OriginalPos As Integer = Sub_Offset
                                        Dim Index As Integer = 0
                                        For Each Subtitle As String In Subs
                                            Dim Text_Length As Integer = Read32(File_Data, OriginalPos)
                                            Dim SubBytes() As Byte = Get_Bytes_From_Text(Subtitle)
                                            Write32(Temp, Temp.Position, SubBytes.Length + 1)
                                            Temp.Write(SubBytes, 0, SubBytes.Length)
                                            Temp.WriteByte(0)
                                            Dim Val As Integer = OriginalPos + 4 + Text_Length
                                            If Index = Text_Count - 1 Then
                                                Temp.Write(File_Data, Val, File_Data.Length - Val)
                                            Else
                                                Temp.Write(File_Data, Val, &H3C)
                                            End If

                                            OriginalPos += Text_Length + &H40
                                            Index += 1
                                            If Index > Text_Count - 1 Then Exit For
                                        Next
                                        File_Data = Temp.ToArray()
                                    End If
                                End If
                            End If

                            If Name_Table(Name_Index) = "DisConv_Blurb" Then
                                Dim Temp As New MemoryStream()
                                Dim Sub_Offset As Integer = If(Read32(File_Data, &H76) And 4, &H9A, &HB3)
                                Dim Text_Length As Integer = Read32(File_Data, Sub_Offset)
                                Dim SubBytes() As Byte = Get_Bytes_From_Text(Match.Groups(1).Value)
                                Temp.Write(File_Data, 0, Sub_Offset)
                                Write32(Temp, Temp.Position, SubBytes.Length + 1)
                                Temp.Write(SubBytes, 0, SubBytes.Length)
                                Temp.WriteByte(0)
                                Dim Val As Integer = Sub_Offset + 4 + Text_Length
                                Temp.Write(File_Data, Val, File_Data.Length - Val)
                                File_Data = Temp.ToArray()
                            End If
                        End If
                    End If

                    Write32(Data, Export_Offset, ObjTypeRef)
                    Write32(Data, Export_Offset + 4, ParentClassRef)
                    Write32(Data, Export_Offset + 8, OwnerRef)
                    Write32(Data, Export_Offset + 12, Name_Index)
                    Write32(Data, Export_Offset + 16, Object_Reference)
                    Write32(Data, Export_Offset + 20, Name_Count)
                    Write64(Data, Export_Offset + 24, Flags_1)
                    Write32(Data, Export_Offset + 32, File_Data.Length)
                    Write32(Data, Export_Offset + 36, File_Offset)
                    Write32(Data, Export_Offset + 40, Exporter_Flags)
                    Write32(Data, Export_Offset + 44, Length)
                    Dim ExData As String = Regex.Match(Content, "<ExData>0x([0-9A-Fa-f]+)</ExData>", RegexOptions.IgnoreCase).Groups(1).Value
                    For Position As Integer = 0 To ExData.Length - 1 Step 2
                        Dim Hex_Value As String = ExData.Substring(Position, 2)
                        Dim Value As Byte = Convert.ToByte(Convert.ToInt32(Hex_Value, 16) And &HFF)
                        Data.WriteByte(Value)
                    Next

                    Console.WriteLine("Inserindo " & Temp_File & "...")
                    Data.Seek(File_Offset, SeekOrigin.Begin)
                    Data.Write(File_Data, 0, File_Data.Length)
                    File_Offset += File_Data.Length
                    Export_Offset += &H44 + (Length * 4)
                Next

                File.WriteAllBytes(File_Name & ".XXX", Data.ToArray())
                Data.Close()
            Else
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine("Arquivo/diretório não encontrado!")
                Console.ReadKey()
                Console.ResetColor()
                End
            End If

            Console.ForegroundColor = ConsoleColor.Green
            Console.WriteLine("Feito!")

            Remaining_Args -= 1
            CurrArg += 1
        Loop While Remaining_Args > 0

        Console.ResetColor()
    End Sub

    Public Function Read64(Data() As Byte, Address As Integer) As UInt64
        Return Convert.ToUInt64(Data(Address + 7) And &HFF) + _
            (Convert.ToUInt64(Data(Address + 6) And &HFF) << 8) + _
            (Convert.ToUInt64(Data(Address + 5) And &HFF) << 16) + _
            (Convert.ToUInt64(Data(Address + 4) And &HFF) << 24) + _
            (Convert.ToUInt64(Data(Address + 3) And &HFF) << 32) + _
            (Convert.ToUInt64(Data(Address + 2) And &HFF) << 40) + _
            (Convert.ToUInt64(Data(Address + 1) And &HFF) << 48) + _
            (Convert.ToUInt64(Data(Address) And &HFF) << 56)
    End Function
    Public Function Read32(Data() As Byte, Address As Integer) As Integer
        Return (Data(Address + 3) And &HFF) + _
            ((Data(Address + 2) And &HFF) << 8) + _
            ((Data(Address + 1) And &HFF) << 16) + _
            ((Data(Address) And &HFF) << 24)
    End Function
    Public Function Read24(Data() As Byte, Address As Integer) As Integer
        Return (Data(Address + 2) And &HFF) + _
            ((Data(Address + 1) And &HFF) << 8) + _
            ((Data(Address) And &HFF) << 16)
    End Function
    Public Function Read16(Data() As Byte, Address As Integer) As Integer
        Return (Data(Address + 1) And &HFF) + _
            ((Data(Address) And &HFF) << 8)
    End Function
    Private Function ReadStr(Data() As Byte, Address As Integer, Length As Integer) As String
        Dim Out As String = Nothing
        For Offset As Integer = Address To Address + Length - 1
            Out &= Chr(Data(Offset))
        Next
        Return Out
    End Function
    Private Function ReadName(Data() As Byte, Address As Integer, Count As Integer, Index As Integer)
        Dim Idx As Integer
        While Idx < Count
            Dim Length As Integer = Read32(Data, Address)
            If Idx = Index Then
                Return ReadStr(Data, Address + 4, Length - 1)
            End If
            Address += 4 + Length + 8

            Idx += 1
        End While
        Return Nothing
    End Function
    Private Function ReadNameFlags(Data() As Byte, Address As Integer, Count As Integer, Index As Integer) As UInt64
        Dim Idx As Integer
        While Idx < Count
            Dim Length As Integer = Read32(Data, Address)
            If Idx = Index Then
                Return Read64(Data, Address + 4 + Length)
            End If
            Address += 4 + Length + 8

            Idx += 1
        End While
        Return Nothing
    End Function

    Private Sub Write64(Stream As Stream, Address As Integer, Value As UInt64)
        Stream.Seek(Address, SeekOrigin.Begin)
        Stream.WriteByte(Convert.ToByte((Value >> 56) And &HFF))
        Stream.WriteByte(Convert.ToByte((Value >> 48) And &HFF))
        Stream.WriteByte(Convert.ToByte((Value >> 40) And &HFF))
        Stream.WriteByte(Convert.ToByte((Value >> 32) And &HFF))
        Stream.WriteByte(Convert.ToByte((Value >> 24) And &HFF))
        Stream.WriteByte(Convert.ToByte((Value >> 16) And &HFF))
        Stream.WriteByte(Convert.ToByte((Value >> 8) And &HFF))
        Stream.WriteByte(Convert.ToByte(Value And &HFF))
    End Sub
    Private Sub Write32(Stream As Stream, Address As Integer, Value As Integer)
        Stream.Seek(Address, SeekOrigin.Begin)
        Stream.WriteByte(Convert.ToByte((Value >> 24) And &HFF))
        Stream.WriteByte(Convert.ToByte((Value >> 16) And &HFF))
        Stream.WriteByte(Convert.ToByte((Value >> 8) And &HFF))
        Stream.WriteByte(Convert.ToByte(Value And &HFF))
    End Sub
    Private Sub Write24(Stream As Stream, Address As Integer, Value As Integer)
        Stream.Seek(Address, SeekOrigin.Begin)
        Stream.WriteByte(Convert.ToByte((Value >> 16) And &HFF))
        Stream.WriteByte(Convert.ToByte((Value >> 8) And &HFF))
        Stream.WriteByte(Convert.ToByte(Value And &HFF))
    End Sub
    Private Sub Write16(Stream As Stream, Address As Integer, Value As Integer)
        Stream.Seek(Address, SeekOrigin.Begin)
        Stream.WriteByte(Convert.ToByte((Value >> 8) And &HFF))
        Stream.WriteByte(Convert.ToByte(Value And &HFF))
    End Sub

    Private Function Get_Bytes_From_Text(Text As String) As Byte()
        Dim Out(Text.Length - 1) As Byte
        For i As Integer = 0 To Text.Length - 1
            Dim Character As String = Text.Substring(i, 1)
            Out(i) = AscW(Character) And &HFF
        Next
        Return Out
    End Function

End Module
