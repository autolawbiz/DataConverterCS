using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jp.autolawbiz.DataConverter
{
    public class Shapefile : Conversion
    {
        public static void ToGeoJSON(string inputPath, string outputPath)
        {
            string shapePath = inputPath; //Shapefileパス
            string dbfPath = shapePath.Replace("shp", "dbf");   //DBFパス
            string jsonPath = outputPath;   //JSONパス

            Dbffile[] dbfFields = ReadDbf(dbfPath);

            //Shapefileを開く
            FileStream fs = new FileStream(shapePath, FileMode.Open);
            BinaryReader br = new BinaryReader(fs);

            int fileNowLength = 0;
            int fileRecordCount = 0;
            int recordNowCount = 0;

            Encoding utf8Enc = Encoding.GetEncoding("UTF-8");
            // （1）テキストファイルを開いて（なければ作って）StreamWriterオブジェクトを得る
            using (StreamWriter writer = new StreamWriter(jsonPath, false, utf8Enc))
            {
                //---メイン・ファイル・ヘッダ---
                //int fileCode = br.ReadInt32();
                int fileCode = Byte2Int(Reverse4ByteInt(BitConverter.GetBytes(br.ReadInt32())));
                SkipReadInt(5, br);
                //ファイル長の値はワード単位（16 ビットを１ワードとする）で、ヘッダの 50 ワードを含む全ファイルの長さです。
                int fileLength = Byte2Int(Reverse4ByteInt(BitConverter.GetBytes(br.ReadInt32())));
                int fileVersion = br.ReadInt32();
                int fileShapeType = br.ReadInt32();
                double fileBoundingBoxXmin = br.ReadDouble();
                double fileBoundingBoxYmin = br.ReadDouble();
                double fileBoundingBoxXmax = br.ReadDouble();
                double fileBoundingBoxYmax = br.ReadDouble();
                double fileBoundingBoxZmin = br.ReadDouble();
                double fileBoundingBoxZmax = br.ReadDouble();
                double fileBoundingBoxMmin = br.ReadDouble();
                double fileBoundingBoxMmax = br.ReadDouble();
                fileNowLength = 50;

                //GeoJSONヘッダ
                writer.WriteLine("{");
                writer.WriteLine("\t\"type\": \"FeatureCollection\",");
                writer.WriteLine("\t\"features\": [");

                while (fileNowLength < fileLength)
                {
                    //---レコード・ヘッダ---
                    int recordNumber = Byte2Int(Reverse4ByteInt(BitConverter.GetBytes(br.ReadInt32())));
                    fileRecordCount = recordNumber;
                    int contentsLength = Byte2Int(Reverse4ByteInt(BitConverter.GetBytes(br.ReadInt32())));
                    int shapeType = br.ReadInt32();
                    int numParts = 0;
                    int numPoints = 0;
                    string vertexPoints = "";

                    //GeoJSONポリゴンヘッダ
                    writer.WriteLine("\t\t{");
                    writer.WriteLine("\t\t\t\"type\": \"Feature\",");

                    //GeoJSON属性ヘッダ
                    writer.WriteLine("\t\t\t\"properties\": {");

                    //GeoJSON属性
                    for (int m = 0; m < dbfFields.Length; m++)
                    {
                        if (m == dbfFields.Length - 1)
                            writer.WriteLine("\t\t\t\t\"" + dbfFields[m].FieldName + "\": \""
                                    + dbfFields[m].ListValues[recordNowCount] + "\"");
                        else
                            writer.WriteLine("\t\t\t\t\"" + dbfFields[m].FieldName + "\": \""
                                    + dbfFields[m].ListValues[recordNowCount] + "\",");
                    }

                    //GeoJSON属性フッタ
                    writer.WriteLine("\t\t\t},");

                    //GeoJSON図形ヘッダ
                    writer.WriteLine("\t\t\t\"geometry\": {");
                    //if (fileShapeType == 1)
                    //{
                    //    writer.WriteLine("\t\t\t\t\"type\": \"Point\",");
                    //    writer.WriteLine("\t\t\t\t\"coordinates\": ");
                    //}
                    //else if (fileShapeType == 3)
                    //{
                    //    writer.WriteLine("\t\t\t\t\"type\": \"LineString\",");
                    //    writer.WriteLine("\t\t\t\t\"coordinates\": [");
                    //}
                    //else if (fileShapeType == 5)
                    //{
                    //    writer.WriteLine("\t\t\t\t\"type\": \"Polygon\",");
                    //    writer.WriteLine("\t\t\t\t\"coordinates\": [");
                    //    writer.WriteLine("\t\t\t\t\t[");
                    //}

                    if (fileShapeType == 1)
                    {
                        writer.WriteLine("\t\t\t\t\"type\": \"Point\",");
                        writer.Write("\t\t\t\t\"coordinates\": ");

                        fileNowLength += 6;
                        vertexPoints += "POINT(";
                        double dblX = br.ReadDouble();
                        double dblY = br.ReadDouble();
                        fileNowLength += 8;
                        vertexPoints += dblX + " " + dblY;
                        vertexPoints += ")";

                        string sLine = "[" + dblX
                            + ", " + dblY
                            + "]";
                        writer.WriteLine(sLine);
                    }
                    else if (fileShapeType == 3)
                    {
                        writer.WriteLine("\t\t\t\t\"type\": \"LineString\",");
                        writer.WriteLine("\t\t\t\t\"coordinates\": [");

                        double boundingBoxXmin = br.ReadDouble();
                        double boundingBoxYmin = br.ReadDouble();
                        double boundingBoxXmax = br.ReadDouble();
                        double boundingBoxYmax = br.ReadDouble();
                        numParts = br.ReadInt32();
                        numPoints = br.ReadInt32();
                        fileNowLength += 26;

                        List<int> vStartPos = new List<int>();
                        for (int i = 0; i < numParts; i++)
                        {
                            int intParts = br.ReadInt32();
                            fileNowLength += 2;
                            vStartPos.Add(intParts);
                        }
                        vertexPoints += "LINESTRING(";
                        string sLine = "";
                        for (int j = 0; j < numPoints; j++)
                        {
                            double dblX = br.ReadDouble();
                            double dblY = br.ReadDouble();
                            fileNowLength += 8;
                            vertexPoints += dblX + " " + dblY + ", ";

                            //最終頂点
                            if (j == numPoints - 1)
                            {
                                sLine += "[" + dblX
                                  + ", " + dblY
                                  + "]";
                            }
                            //最終頂点以外の頂点
                            else
                            {
                                sLine += "[" + dblX
                                  + ", " + dblY
                                  + "], ";
                            }

                            //GeoJSON頂点（5頂点ごとに改行）
                            if (j % 5 == 4)
                            {
                                //if (fileShapeType == 3)
                                writer.WriteLine("\t\t\t\t\t" + sLine);
                                //else if (fileShapeType == 5)
                                //    writer.WriteLine("\t\t\t\t\t\t" + sLine);
                                sLine = "";
                            }
                            else if (j == numPoints - 1)
                            {
                                //if (fileShapeType == 3)
                                writer.WriteLine("\t\t\t\t\t" + sLine);
                                //else if (fileShapeType == 5)
                                //    writer.WriteLine("\t\t\t\t\t\t" + sLine);
                            }
                        }
                        vertexPoints = vertexPoints.Substring(0, vertexPoints.Length - 2);
                        vertexPoints += ")";
                    }
                    else if (fileShapeType == 5)
                    {
                        writer.WriteLine("\t\t\t\t\"type\": \"Polygon\",");
                        writer.WriteLine("\t\t\t\t\"coordinates\": [");
                        writer.WriteLine("\t\t\t\t\t[");

                        double boundingBoxXmin = br.ReadDouble();
                        double boundingBoxYmin = br.ReadDouble();
                        double boundingBoxXmax = br.ReadDouble();
                        double boundingBoxYmax = br.ReadDouble();
                        numParts = br.ReadInt32();
                        numPoints = br.ReadInt32();
                        fileNowLength += 26;

                        List<int> vStartPos = new List<int>();
                        for (int i = 0; i < numParts; i++)
                        {
                            int intParts = br.ReadInt32();
                            fileNowLength += 2;
                            vStartPos.Add(intParts);
                        }
                        vertexPoints += "POLYGON(";
                        if (numParts == 1)
                        {
                            string sLine = "";
                            vertexPoints += "(";
                            for (int j = 0; j < numPoints; j++)
                            {
                                double dblX = br.ReadDouble();
                                double dblY = br.ReadDouble();
                                fileNowLength += 8;
                                vertexPoints += dblX + " " + dblY + ", ";

                                //最終頂点
                                if (j == numPoints - 1)
                                {
                                    sLine += "[" + dblX
                                      + ", " + dblY
                                      + "]";
                                }
                                //最終頂点以外の頂点
                                else
                                {
                                    sLine += "[" + dblX
                                      + ", " + dblY
                                      + "], ";
                                }

                                //GeoJSON頂点（5頂点ごとに改行）
                                if (j % 5 == 4)
                                {
                                    //if (fileShapeType == 3)
                                    //    writer.WriteLine("\t\t\t\t\t" + sLine);
                                    //else if (fileShapeType == 5)
                                    writer.WriteLine("\t\t\t\t\t\t" + sLine);
                                    sLine = "";
                                }
                                else if (j == numPoints - 1)
                                {
                                    //if (fileShapeType == 3)
                                    //    writer.WriteLine("\t\t\t\t\t" + sLine);
                                    //else if (fileShapeType == 5)
                                    writer.WriteLine("\t\t\t\t\t\t" + sLine);
                                }
                            }
                            vertexPoints = vertexPoints.Substring(0, vertexPoints.Length - 2);
                            vertexPoints += ")";
                        }
                        else if (numParts > 1)
                        {
                            for (int k = 0; k < numParts; k++)
                            {
                                if (k == numParts - 1)
                                {
                                    string sLine = "";
                                    //最後の点列
                                    int startPos = 0;
                                    int endPos = numPoints - (vStartPos[k] + 1);
                                    vertexPoints += "(";
                                    for (int l = startPos; l <= endPos; l++)
                                    {
                                        double dblX = br.ReadDouble();
                                        double dblY = br.ReadDouble();
                                        fileNowLength += 8;
                                        vertexPoints += dblX + " " + dblY + ", ";

                                        //最終頂点
                                        if (l == endPos)
                                        {
                                            sLine += "[" + dblX
                                              + ", " + dblY
                                              + "]";
                                        }
                                        //最終頂点以外の頂点
                                        else
                                        {
                                            sLine += "[" + dblX
                                              + ", " + dblY
                                              + "], ";
                                        }

                                        //GeoJSON頂点（5頂点ごとに改行）
                                        if (l % 5 == 4)
                                        {
                                            //if (fileShapeType == 3)
                                            //    writer.WriteLine("\t\t\t\t\t" + sLine);
                                            //else if (fileShapeType == 5)
                                            writer.WriteLine("\t\t\t\t\t\t" + sLine);
                                            sLine = "";
                                        }
                                        else if (l == endPos)
                                        {
                                            //if (fileShapeType == 3)
                                            //    writer.WriteLine("\t\t\t\t\t" + sLine);
                                            //else if (fileShapeType == 5)
                                            writer.WriteLine("\t\t\t\t\t\t" + sLine);
                                        }
                                    }
                                    vertexPoints = vertexPoints.Substring(0, vertexPoints.Length - 2);
                                    vertexPoints += ")";
                                }
                                else
                                {
                                    string sLine = "";
                                    //最初、中間の点列
                                    int startPos = 0;
                                    int endPos = vStartPos[k + 1] - (vStartPos[k] + 1);
                                    vertexPoints += "(";
                                    for (int l = startPos; l <= endPos; l++)
                                    {
                                        double dblX = br.ReadDouble();
                                        double dblY = br.ReadDouble();
                                        fileNowLength += 8;
                                        vertexPoints += dblX + " " + dblY + ", ";

                                        //最終頂点
                                        if (l == endPos)
                                        {
                                            sLine += "[" + dblX
                                              + ", " + dblY
                                              + "]";
                                        }
                                        //最終頂点以外の頂点
                                        else
                                        {
                                            sLine += "[" + dblX
                                              + ", " + dblY
                                              + "], ";
                                        }

                                        //GeoJSON頂点（5頂点ごとに改行）
                                        if (l % 5 == 4)
                                        {
                                            //if (fileShapeType == 3)
                                            //    writer.WriteLine("\t\t\t\t\t" + sLine);
                                            //else if (fileShapeType == 5)
                                            writer.WriteLine("\t\t\t\t\t\t" + sLine);
                                            sLine = "";
                                        }
                                        else if (l == endPos)
                                        {
                                            //if (fileShapeType == 3)
                                            //    writer.WriteLine("\t\t\t\t\t" + sLine);
                                            //else if (fileShapeType == 5)
                                            writer.WriteLine("\t\t\t\t\t\t" + sLine);
                                        }

                                        if (l == endPos)
                                        {
                                            //GeoJSON図形フッタ
                                            writer.WriteLine("\t\t\t\t\t],");
                                            //GeoJSON図形ヘッダ
                                            writer.WriteLine("\t\t\t\t\t[");
                                        }
                                    }
                                    vertexPoints = vertexPoints.Substring(0, vertexPoints.Length - 2);
                                    vertexPoints += "), ";
                                }
                            }
                        }
                        vertexPoints += ")";
                    }
                    //writer.WriteLine("ShapeType:" + shapeType + "⇒" + vertexPoints);
                    //string strInsertSql = strPrefix + vertexPoints + strSuffix;
                    //Submit_Tsql_NonQuery(connection, "3 - Inserts", strInsertSql);

                    //GeoJSON図形フッタ
                    if (fileShapeType == 1)
                    {
                        writer.WriteLine("\t\t\t}");
                    }
                    else if (fileShapeType == 3)
                    {
                        writer.WriteLine("\t\t\t\t]");
                        writer.WriteLine("\t\t\t}");
                    }
                    else if (fileShapeType == 5)
                    {
                        writer.WriteLine("\t\t\t\t\t]");
                        writer.WriteLine("\t\t\t\t]");
                        writer.WriteLine("\t\t\t}");
                    }

                    //GeoJSONポリゴンフッタ
                    //最終ポリゴン
                    if (fileNowLength == fileLength)
                    {
                        writer.WriteLine("\t\t}");
                    }
                    //最終ポリゴン以外のポリゴン
                    else
                    {
                        writer.WriteLine("\t\t},");
                    }

                    recordNowCount++;
                    //strInsertSql += strPrefix + vertexPoints + strSuffix + "\n";
                    //if (((recordNowCount % 100) == 0) || (fileRecordCount == recordNowCount))
                    //{
                    //    Submit_Tsql_NonQuery(connection, "3 - Inserts", strInsertSql);
                    //    strInsertSql = "";
                    //}

                }

                //GeoJSONフッタ
                writer.WriteLine("\t]");
                writer.WriteLine("}");

                Console.WriteLine("File Length " + fileLength);
                Console.WriteLine("Real File Length " + fileNowLength);
                Console.WriteLine("Record Number " + fileRecordCount);
                Console.WriteLine("Real Record Number " + recordNowCount);
                Console.WriteLine("File Shape Type " + fileShapeType);

                //Shapefileを閉じる
                br.Close();
                fs.Close();

                Console.WriteLine("Fin.");
            }
        }

        private static Dbffile[] ReadDbf(string recdbf)
        {
            //Dbffileを開く
            FileStream fs = new FileStream(recdbf, FileMode.Open);
            BinaryReader br = new BinaryReader(fs);

            //---メイン・ファイル・ヘッダ---
            byte fileInfo = br.ReadByte();
            //ファイル長の値はワード単位（16 ビットを１ワードとする）で、ヘッダの 50 ワードを含む全ファイルの長さです。
            byte fileLastUpdate1 = br.ReadByte();
            byte fileLastUpdate2 = br.ReadByte();
            byte fileLastUpdate3 = br.ReadByte();
            int fileRecordNum = br.ReadInt32(); ;
            short fileHeaderBytes = br.ReadInt16();
            short fileRecordBytes = br.ReadInt16();
            SkipReadByte(20, br);

            int fieldNum = (fileHeaderBytes - 33) / 32;
            //int fieldNum = fileRecordNum / 32;
            Dbffile[] dbfFields = new Dbffile[fieldNum];
            for (int i = 0; i < fieldNum; i++)
            {
                dbfFields[i] = ReadFields(br);
            }

            //フィールドの終わりを示す符号（0DH）
            SkipReadByte(1, br);

            for (int j = 0; j < fileRecordNum; j++)
            {
                /*
				 * データ・レコードの先頭の１バイトが
				 * 半角空白（20H）のとき、このレコードは削除されていないことをあらわす。アスタリスク（2AH）の
				 * とき、 このレコードは削除されていることをあらわす。
				 * */
                SkipReadByte(1, br);
                for (int k = 0; k < fieldNum; k++)
                {
                    int fieldLength = dbfFields[k].FieldLength;
                    byte[] valueByte = new byte[fieldLength];
                    for (int l = 0; l < fieldLength; l++)
                    {
                        valueByte[l] = br.ReadByte();
                    }
                    string strValue = "";
                    if (dbfFields[k].FieldType.Equals("N"))
                    {
                        strValue = System.Text.Encoding.ASCII.GetString(valueByte);// ASCII
                    }
                    else if (dbfFields[k].FieldType.Equals("C"))
                    {
                        strValue = System.Text.Encoding.GetEncoding(932).GetString(valueByte);// SJIS
                    }
                    //List<string> listValues = dbfFields[k].ListValues;
                    //listValues.Add(strValue.Trim());
                    dbfFields[k].ListValues.Add(strValue.Trim());
                }
            }

            return dbfFields;
        }

        private static Dbffile ReadFields(BinaryReader recbr)
        {
            var listFieldNameBytes = new List<byte>();
            string fieldName = "";

            //byte[] fieldNameArray = new byte[11];
            //for (int k = 10; k >= 0; k--)
            //{
            //    fieldNameArray[k] = recbr.ReadByte();
            //}

            for (int i = 0; i < 11; i++)
            {
                byte fieldNameChar = recbr.ReadByte();
                //byte fieldNameChar = fieldNameArray[i];
                if (fieldNameChar == 0)
                {
                    SkipReadByte(10 - i, recbr);
                    break;
                }
                else
                    listFieldNameBytes.Add(fieldNameChar);
            }

            int fieldNameSize = listFieldNameBytes.Count;
            byte[] fieldNameBytes = new byte[fieldNameSize];
            //int m = 0;
            for (int j = 0; j < fieldNameSize; j++)
            //for (int j = fieldNameSize - 1; j >= 0; j--)
            {
                fieldNameBytes[j] = listFieldNameBytes[j];
                //m++;
            }
            // US-ASCII
            //fieldName = System.Text.Encoding.GetEncoding(20127).GetString(fieldNameBytes);
            fieldName = System.Text.Encoding.ASCII.GetString(fieldNameBytes);

            byte[] fieldTypeByte = { 0 };
            string fieldType = "";
            fieldTypeByte[0] = recbr.ReadByte();
            // US-ASCII
            //fieldType = System.Text.Encoding.GetEncoding(20127).GetString(fieldTypeByte);
            fieldType = System.Text.Encoding.ASCII.GetString(fieldTypeByte);

            SkipReadByte(4, recbr);

            //byte[] fieldLengthByte = { 0 };
            //int fieldLength = 0;
            //fieldLengthByte[0] = recbr.ReadByte();
            //fieldLength = Byte2Int(fieldLengthByte);
            int fieldLength = recbr.ReadByte();

            //byte[] fieldDecimalpartLengthByte = { 0 };
            //int fieldDecimalpartLength = 0;
            //fieldDecimalpartLengthByte[0] = recbr.ReadByte();
            //fieldDecimalpartLength = Byte2Int(fieldDecimalpartLengthByte);
            int fieldDecimalpartLength = recbr.ReadByte();

            Dbffile dbfFields = new Dbffile(new List<string>())
            {
                FieldName = fieldName,
                FieldType = fieldType,
                FieldLength = fieldLength,
                FieldDecimalpartLength = fieldDecimalpartLength
            };

            SkipReadByte(14, recbr);

            return dbfFields;
        }
    }
}
