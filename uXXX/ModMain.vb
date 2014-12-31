Imports System.IO
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
        Console.WriteLine("uXXX v0.1 by gdkchan")
        Dim Args() As String = Environment.GetCommandLineArgs
        Dim File_Name As String
        If Args.Count < 2 Then
            Console.ForegroundColor = ConsoleColor.White
            Console.WriteLine("Digite o nome do arquivo:")
            Console.ResetColor()
            File_Name = Console.ReadLine
        Else
            File_Name = Args(1)
        End If
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine("------")

        If File.Exists(File_Name) Then
            Dim Data() As Byte = File.ReadAllBytes(File_Name)

            Dim Magic As Integer = Read32(Data, 0)
            If Magic = &H9E2A83C1 Then
                Dim Header_XML As New StringBuilder
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

                Dim GensCount As Integer = Read32(Data, Base_Offset + 64)
                Dim Engine_Version As Integer = Read32(Data, Base_Offset + 80)
                Dim Cooker_Version As Integer = Read32(Data, Base_Offset + 84)
                Dim Data_2 As Integer = Read32(Data, Base_Offset + 96)

                Header_XML.AppendLine(TAB & "<EngineVersion>" & Engine_Version & "</EngineVersion>")
                Header_XML.AppendLine(TAB & "<CookerVersion>" & Cooker_Version & "</CookerVersion>")
                Header_XML.AppendLine(TAB & "<GenerationsCount>" & GensCount & "</GenerationsCount>")
                Header_XML.AppendLine(TAB & "<ExFlags>" & "0x" & Hex(Data_2).PadLeft(8, "0"c) & "</ExFlags>") 'Não sei o que é D:
                Header_XML.AppendLine(TAB & "<ZeroPadLength>" & Zero_Pad_Length & "</ZeroPadLength>") 'Não sei o que é D:

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
                    Dim FType As Integer = Read32(Data, EOffset)
                    Dim Length As Integer = Read32(Data, EOffset + 44)
                    Dim Entry_Length As Integer = &H44 + (Length * 4)

                    Dim Reference_Index As Integer = Read32(Data, EOffset + 8)
                    Dim Name_Index As Integer = Read32(Data, EOffset + 12)
                    Dim Object_Reference As Integer = Read32(Data, EOffset + 16) - 1
                    Dim Name As String = ReadName(Data, Names_Offset, Names_Count, Name_Index)
                    If Object_Reference > -1 Then Name &= "_" & Object_Reference
                    Dim Flags_1 As UInt64 = Read64(Data, EOffset + 24)
                    Dim File_Length As Integer = Read32(Data, EOffset + 32)
                    Dim File_Offset As Integer = Read32(Data, EOffset + 36)
                    Dim Exporter_Flags As Integer = Read32(Data, EOffset + 40)
                    '32 bytes que eu não sei o que é (pelo menos no Package)

                    Header_XML.AppendLine(TAB_2 & "<Entry>")
                    Header_XML.AppendLine(TAB_3 & "<File>" & Name & "</File>")
                    Header_XML.AppendLine(TAB_3 & "<NameIndex>" & Name_Index & "</NameIndex>" & " <!--" & ReadName(Data, Names_Offset, Names_Count, Name_Index) & "-->")
                    Header_XML.AppendLine(TAB_3 & "<Class>" & FType & "</Class>")
                    Header_XML.AppendLine(TAB_3 & "<ObjReference>" & Object_Reference & "</ObjReference>")
                    Header_XML.AppendLine(TAB_3 & "<SNWIndex>" & Reference_Index & "</SNWIndex>")
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

                    Header_XML.AppendLine(TAB_2 & "<Entry>")
                    Header_XML.AppendLine(TAB_3 & "<PackageNameIndex>" & Package_Index & "</PackageNameIndex>" & " <!--" & ReadName(Data, Names_Offset, Names_Count, Package_Index) & "-->")
                    Header_XML.AppendLine(TAB_3 & "<ClassNameIndex>" & Class_Index & "</ClassNameIndex>" & " <!--" & ReadName(Data, Names_Offset, Names_Count, Class_Index) & "-->")
                    Header_XML.AppendLine(TAB_3 & "<ObjectNameIndex>" & Object_Index & "</ObjectNameIndex>" & " <!--" & ReadName(Data, Names_Offset, Names_Count, Object_Index) & "-->")
                    Header_XML.AppendLine(TAB_3 & "<Outer>" & Outer & "</Outer>")
                    Header_XML.AppendLine(TAB_2 & "</Entry>")

                    IOffset += 28
                Next

                Header_XML.AppendLine(TAB & "</Import>")

                Header_XML.AppendLine("</Header>")

                Console.WriteLine("Salvando ""Header.xml""...")
                File.WriteAllText(Path.Combine(Work_Dir, "Header.xml"), Header_XML.ToString)
            Else
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine("Esse arquivo não é XXX!")
                Console.ReadKey()
                End
            End If
        ElseIf Directory.Exists(File_Name) Then
            If Not File.Exists(Path.Combine(File_Name, "Header.xml")) Then
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine("Arquivo ""Header.xml"" não encontrado!")
                Console.ReadKey()
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
            Dim Bytes() As Byte = Encoding.UTF8.GetBytes(Folder_Name)
            Write32(Data, 12, Bytes.Length + 1)
            Data.Write(Bytes, 0, Bytes.Length)
            Data.WriteByte(0)
            Dim Base_Offset As Integer = Data.Position
            Dim Flags As Integer = Convert.ToInt32(Regex.Match(Header_Section, "<Flags>0x([0-9A-Fa-f]+)</Flags>", RegexOptions.IgnoreCase).Groups(1).Value, 16)
            Write32(Data, Base_Offset, Flags)
            Dim GUID As String = Regex.Match(Header_Section, "<GUID>0x([0-9A-Fa-f]+)</GUID>", RegexOptions.IgnoreCase).Groups(1).Value
            For Position As Integer = 0 To GUID.Length - 1 Step 2
                Dim Hex_Value As String = GUID.Substring(Position, 2)
                Dim Value As Byte = Convert.ToByte(Convert.ToInt32(Hex_Value, 16) And &HFF)
                Data.WriteByte(Value)
            Next
            Dim Generations_Count As Integer = Integer.Parse(Regex.Match(Header_Section, "<GenerationsCount>(\d+)</GenerationsCount>", RegexOptions.IgnoreCase).Groups(1).Value)
            Dim Engine_Version As Integer = Integer.Parse(Regex.Match(Header_Section, "<EngineVersion>(\d+)</EngineVersion>", RegexOptions.IgnoreCase).Groups(1).Value)
            Dim Cooker_Version As Integer = Integer.Parse(Regex.Match(Header_Section, "<CookerVersion>(\d+)</CookerVersion>", RegexOptions.IgnoreCase).Groups(1).Value)
            Dim ExFlags As Integer = Convert.ToInt32(Regex.Match(Header_Section, "<ExFlags>0x([0-9A-Fa-f]+)</ExFlags>", RegexOptions.IgnoreCase).Groups(1).Value, 16)

            Write32(Data, Base_Offset + 64, Generations_Count)
            Write32(Data, Base_Offset + (Generations_Count * 12) + 68, Engine_Version)
            Write32(Data, Base_Offset + (Generations_Count * 12) + 72, Cooker_Version)
            Write32(Data, Base_Offset + (Generations_Count * 12) + 84, ExFlags)

            Dim Names_Offset As Integer = Base_Offset + (Generations_Count * 12) + 92
            Dim Names_Section As String = Regex.Match(Header_Section, "<Names>(.+?)</Names>", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Groups(1).Value
            Dim Names_Entries As MatchCollection = Regex.Matches(Names_Section, "<Entry>(.+?)</Entry>", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
            Write32(Data, Base_Offset + 4, Names_Entries.Count)
            If Generations_Count > 0 Then Write32(Data, Base_Offset + 72, Names_Entries.Count)
            Write32(Data, Base_Offset + 8, Names_Offset)
            For Each Entry As Match In Names_Entries
                Dim Content As String = Entry.Groups(1).Value
                Dim Text As String = Regex.Match(Content, "<Text>(.+?)</Text>", RegexOptions.IgnoreCase).Groups(1).Value
                Dim Text_Flags As UInt64 = Convert.ToUInt64(Regex.Match(Content, "<Flags>0x([0-9A-Fa-f]+)</Flags>", RegexOptions.IgnoreCase).Groups(1).Value, 16)

                Dim Text_Bytes() As Byte = Encoding.UTF8.GetBytes(Text)
                Write32(Data, Names_Offset, Text_Bytes.Length + 1)
                Data.Write(Text_Bytes, 0, Text_Bytes.Count)
                Data.WriteByte(0)
                Names_Offset = Data.Position
                Write64(Data, Names_Offset, Text_Flags)
                Names_Offset += 8
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

                Write32(Data, Import_Offset, Package_Index)
                Write32(Data, Import_Offset + 8, Class_Index)
                Write32(Data, Import_Offset + 16, Outer)
                Write32(Data, Import_Offset + 20, Object_Index)

                Import_Offset += 28
            Next

            Dim Export_Offset As Integer = Import_Offset
            Dim Export_Section As String = Regex.Match(Header_Section, "<Export>(.+?)</Export>", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Groups(1).Value
            Dim Export_Entries As MatchCollection = Regex.Matches(Export_Section, "<Entry>(.+?)</Entry>", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
            Write32(Data, Base_Offset + 12, Export_Entries.Count)
            If Generations_Count > 0 Then Write32(Data, Base_Offset + 68, Export_Entries.Count)
            Write32(Data, Base_Offset + 16, Export_Offset)

            Dim Export_Length As Integer
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
                    End
                End If
                Dim File_Data() As Byte = File.ReadAllBytes(Path.Combine(File_Name, Temp_File))
                Dim FType As Integer = Integer.Parse(Regex.Match(Content, "<Class>([-]?\d+)</Class>", RegexOptions.IgnoreCase).Groups(1).Value)
                Dim Name_Index As Integer = Integer.Parse(Regex.Match(Content, "<NameIndex>(\d+)</NameIndex>", RegexOptions.IgnoreCase).Groups(1).Value)
                Dim Object_Reference As Integer = Integer.Parse(Regex.Match(Content, "<ObjReference>([-]?\d+)</ObjReference>", RegexOptions.IgnoreCase).Groups(1).Value) + 1
                Dim Reference_Index As Integer = Integer.Parse(Regex.Match(Content, "<SNWIndex>([-]?\d+)</SNWIndex>", RegexOptions.IgnoreCase).Groups(1).Value)
                Dim Flags_1 As UInt64 = Convert.ToUInt64(Regex.Match(Content, "<Flags>0x([0-9A-Fa-f]+)</Flags>", RegexOptions.IgnoreCase).Groups(1).Value, 16)
                Dim Exporter_Flags As Integer = Convert.ToInt32(Regex.Match(Content, "<ExporterFlags>0x([0-9A-Fa-f]+)</ExporterFlags>", RegexOptions.IgnoreCase).Groups(1).Value, 16)
                Dim Length As Integer = Integer.Parse(Regex.Match(Content, "<EntryLength>([-]?\d+)</EntryLength>", RegexOptions.IgnoreCase).Groups(1).Value)

                Write32(Data, Export_Offset, FType)
                Write32(Data, Export_Offset + 8, Reference_Index)
                Write32(Data, Export_Offset + 12, Name_Index)
                Write32(Data, Export_Offset + 16, Object_Reference)
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

                Data.Seek(File_Offset, SeekOrigin.Begin)
                Data.Write(File_Data, 0, File_Data.Length)
                File_Offset += File_Data.Length
                Export_Offset += &H44 + (Length * 4)
            Next

            File.WriteAllBytes(File_Name & ".XXX", Data.ToArray())
        Else
            Console.ForegroundColor = ConsoleColor.Red
            Console.WriteLine("Arquivo/diretório não encontrado!")
            Console.ReadKey()
            End
        End If

        Console.ForegroundColor = ConsoleColor.Green
        Console.WriteLine("Feito!")
        Console.ReadKey()
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

End Module
