using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace jp.autolawbiz.DataConverter
{
    public class Kibanchizu
    {
        public static void ToGeoJSON(string inputPath, string outputPath)
        {
            FileStream fs = null;
            XmlReader xmlReader = null;
            XmlReaderSettings settings = null;

            string strOutput = "";

            try
            {
                // inputの基盤地図情報基本項目
                fs = new FileStream(inputPath, FileMode.Open);

                // outputのGeoJSON
                string jsonPath = outputPath;


                // outputのGeoJSONファイルを開いて（なければ作って）StreamWriterオブジェクトを得る
                Encoding utf8Enc = Encoding.GetEncoding("UTF-8");
                using (StreamWriter writer = new StreamWriter(jsonPath, false, utf8Enc))
                {
                    // outputのGeoJSONヘッダ
                    writer.WriteLine("{");
                    writer.WriteLine("\t\"type\": \"FeatureCollection\",");
                    writer.WriteLine("\t\"features\": [");

                    // inputのXmlReader設定
                    settings = new XmlReaderSettings
                    {
                        IgnoreComments = true,
                        IgnoreWhitespace = true
                    };

                    int itemCounter = 0;
                    string strLocalName = "";

                    var gisAttrTable = new Dictionary<string, string>();
                    //var gmlAttrTable = new Dictionary<string, string>();
                    string posExteriorOutput = "";
                    int flagExterior = 0;
                    string posInteriorOutput = "";
                    int flagInterior = 0;

                    // inputのXmlReader構築
                    xmlReader = XmlReader.Create(fs, settings);
                    // SAXによるXML input
                    while (xmlReader.Read() == true)
                    {
                        // NodeType取得
                        XmlNodeType nType = xmlReader.NodeType;
                        string strNType = nType.ToString();

                        // LocalName取得
                        string strCheckLocalName = xmlReader.LocalName;
                        if ((strCheckLocalName.Length > 0) && (!strCheckLocalName.Equals("")) && (!strCheckLocalName.Equals("timePosition")))
                            strLocalName = strCheckLocalName;

                        // Depth取得
                        int intDepth = xmlReader.Depth;

                        // Name取得
                        string strName = xmlReader.Name;

                        // Elementの開始タグで変数初期化
                        if (strNType.Equals("Element") &&
                            (strLocalName.Equals("AdmArea") || strLocalName.Equals("AdmBdry") || strLocalName.Equals("AdmPt") ||
                            strLocalName.Equals("BldA") || strLocalName.Equals("BldL")) &&
                            intDepth == 1)
                        {
                            gisAttrTable = new Dictionary<string, string>();
                            //gmlAttrTable = new Dictionary<string, string>();
                            posExteriorOutput = "";
                            flagExterior = 0;
                            posInteriorOutput = "";
                            flagInterior = 0;
                        }

                        // exteriorのElementの開始タグでフラグ立てる
                        if (strNType.Equals("Element") && (strLocalName.Equals("exterior") || strLocalName.Equals("gml:exterior")) && intDepth == 6)
                        {
                            flagExterior = 1;
                        }

                        // interiorのElementの開始タグでフラグ立てる
                        if (strNType.Equals("Element") && (strLocalName.Equals("interior") || strLocalName.Equals("gml:interior")) && intDepth == 6)
                        {
                            flagInterior = 1;
                        }

                        // タグ内に値を持っていたら
                        if (xmlReader.HasValue == true)
                        {
                            // GIS座標値：Polygonの点列を取得
                            if (strNType.Equals("Text") && strLocalName.Equals("posList") && intDepth == 13)
                            {

                                // outputのGeoJSONに書き出し
                                if (flagExterior == 1)
                                {
                                    string posValues = xmlReader.Value;
                                    string[] posLines = posValues.Split(new string[] { "\n" }, StringSplitOptions.None);
                                    int returnCount = 0;

                                    posExteriorOutput += "\t\t\t\t\t[\n";

                                    for (int i = 0; i < posLines.Length; i++)
                                    {
                                        if ((posLines[i].Length > 0) && (!posLines[i].Equals("")))
                                        {
                                            returnCount++;
                                            string[] posBls = posLines[i].Split(new string[] { " " }, StringSplitOptions.None);
                                            if (returnCount % 5 == 0)
                                                posExteriorOutput += "[" + posBls[1] + ", " + posBls[0] + "], \n";
                                            else if (returnCount % 5 == 1)
                                                posExteriorOutput += "\t\t\t\t\t\t[" + posBls[1] + ", " + posBls[0] + "], ";
                                            else
                                                posExteriorOutput += "[" + posBls[1] + ", " + posBls[0] + "], ";
                                        }
                                    }
                                    if (returnCount % 5 == 0)
                                        posExteriorOutput = posExteriorOutput.Substring(0, posExteriorOutput.Length - 3);
                                    else
                                        posExteriorOutput = posExteriorOutput.Substring(0, posExteriorOutput.Length - 2);

                                    posExteriorOutput += "\n\t\t\t\t\t]";
                                }

                                if (flagInterior == 1)
                                {
                                    string posValues = xmlReader.Value;
                                    string[] posLines = posValues.Split(new string[] { "\n" }, StringSplitOptions.None);
                                    int returnCount = 0;

                                    posInteriorOutput += ",\n\t\t\t\t\t[\n";

                                    for (int i = 0; i < posLines.Length; i++)
                                    {
                                        if ((posLines[i].Length > 0) && (!posLines[i].Equals("")))
                                        {
                                            returnCount++;
                                            string[] posBls = posLines[i].Split(new string[] { " " }, StringSplitOptions.None);
                                            if (returnCount % 5 == 0)
                                                posInteriorOutput += "[" + posBls[1] + ", " + posBls[0] + "], \n";
                                            else if (returnCount % 5 == 1)
                                                posInteriorOutput += "\t\t\t\t\t\t[" + posBls[1] + ", " + posBls[0] + "], ";
                                            else
                                                posInteriorOutput += "[" + posBls[1] + ", " + posBls[0] + "], ";
                                        }
                                    }
                                    if (returnCount % 5 == 0)
                                        posInteriorOutput = posInteriorOutput.Substring(0, posInteriorOutput.Length - 3);
                                    else
                                        posInteriorOutput = posInteriorOutput.Substring(0, posInteriorOutput.Length - 2);

                                    posInteriorOutput += "\n\t\t\t\t\t]";
                                }
                            }

                            // GIS座標値：LineStringの点列を取得
                            if (strNType.Equals("Text") && strLocalName.Equals("posList") && intDepth == 7)
                            {
                                if (flagExterior == 0 && flagInterior == 0)
                                {
                                    string posValues = xmlReader.Value;
                                    string[] posLines = posValues.Split(new string[] { "\n" }, StringSplitOptions.None);
                                    int returnCount = 0;

                                    //posExteriorOutput += "\t\t\t\t\t[\n";

                                    for (int i = 0; i < posLines.Length; i++)
                                    {
                                        if ((posLines[i].Length > 0) && (!posLines[i].Equals("")))
                                        {
                                            returnCount++;
                                            string[] posBls = posLines[i].Split(new string[] { " " }, StringSplitOptions.None);
                                            if (returnCount % 5 == 0)
                                                posExteriorOutput += "[" + posBls[1] + ", " + posBls[0] + "], \n";
                                            else if (returnCount % 5 == 1)
                                                posExteriorOutput += "\t\t\t\t\t[" + posBls[1] + ", " + posBls[0] + "], ";
                                            else
                                                posExteriorOutput += "[" + posBls[1] + ", " + posBls[0] + "], ";
                                        }
                                    }
                                    if (returnCount % 5 == 0)
                                        posExteriorOutput = posExteriorOutput.Substring(0, posExteriorOutput.Length - 3);
                                    else
                                        posExteriorOutput = posExteriorOutput.Substring(0, posExteriorOutput.Length - 2);

                                    //posExteriorOutput += "\n\t\t\t\t\t]";
                                }
                            }

                            // GIS座標値：Pointの点を取得
                            if (strNType.Equals("Text") && strLocalName.Equals("pos") && intDepth == 5)
                            {
                                if (flagExterior == 0 && flagInterior == 0)
                                {
                                    string posValues = xmlReader.Value;
                                    string[] posLines = posValues.Split(new string[] { "\n" }, StringSplitOptions.None);
                                    int returnCount = 0;

                                    //posExteriorOutput += "\t\t\t\t\t[\n";

                                    for (int i = 0; i < posLines.Length; i++)
                                    {
                                        if ((posLines[i].Length > 0) && (!posLines[i].Equals("")))
                                        {
                                            returnCount++;
                                            string[] posBls = posLines[i].Split(new string[] { " " }, StringSplitOptions.None);
                                            if (returnCount % 5 == 0)
                                                posExteriorOutput += "[" + posBls[1] + ", " + posBls[0] + "], \n";
                                            else if (returnCount % 5 == 1)
                                            {
                                                //posExteriorOutput += "\t\t\t\t\t[" + posBls[1] + ", " + posBls[0] + "], ";
                                                posExteriorOutput += "[" + posBls[1] + ", " + posBls[0] + "], ";
                                            }
                                            else
                                                posExteriorOutput += "[" + posBls[1] + ", " + posBls[0] + "], ";
                                        }
                                    }
                                    if (returnCount % 5 == 0)
                                        posExteriorOutput = posExteriorOutput.Substring(0, posExteriorOutput.Length - 3);
                                    else
                                        posExteriorOutput = posExteriorOutput.Substring(0, posExteriorOutput.Length - 2);

                                    //posExteriorOutput += "\n\t\t\t\t\t]";
                                }
                            }

                            // GIS属性を取得
                            if (strLocalName.Equals("fid") ||
                                strLocalName.Equals("lfSpanFr") || strLocalName.Equals("devDate") ||
                                strLocalName.Equals("orgGILvl") || strLocalName.Equals("vis") ||
                                strLocalName.Equals("type") || strLocalName.Equals("name") || strLocalName.Equals("admCode"))
                            {
                                if (strNType.Equals("Text")/* && intDepth == 3*/)
                                {
                                    string strValue = xmlReader.Value;
                                    //Console.WriteLine(strValue);
                                    if (!gisAttrTable.ContainsKey(strLocalName))
                                    {
                                        gisAttrTable.Add(strLocalName, strValue);
                                    }
                                }
                            }
                        }

                        // タグ内に属性を持っていたら
                        if (xmlReader.HasAttributes == true)
                        {
                            for (int i = 0; i < xmlReader.AttributeCount; i++)
                            {
                                xmlReader.MoveToAttribute(i);
                                //strOutput += "Attribute Name: " + xmlReader.Name + "\r\n";
                                string strKey = strLocalName + "_" + xmlReader.Name;
                                if (xmlReader.HasValue == true)
                                {
                                    Type valueType = xmlReader.ValueType;
                                    //strOutput += "ValueType: " + valueType.ToString() + "\r\n";
                                    //strOutput += "Attribute Value: " + xmlReader.Value + "\r\n";
                                    if (gisAttrTable.ContainsKey(strKey))
                                    {
                                        gisAttrTable[strKey] += "," + xmlReader.Value;
                                    }
                                    else
                                    {
                                        gisAttrTable.Add(strKey, xmlReader.Value);
                                    }
                                }
                            }
                            xmlReader.MoveToElement();
                        }

                        // interiorのElementの終了タグでフラグ初期化
                        if (strNType.Equals("EndElement") && (strLocalName.Equals("interior") || strLocalName.Equals("gml:interior")) && intDepth == 6)
                        {
                            flagInterior = 0;
                        }

                        // exteriorのElementの終了タグでフラグ初期化
                        if (strNType.Equals("EndElement") && (strLocalName.Equals("exterior") || strLocalName.Equals("gml:exterior")) && intDepth == 6)
                        {
                            flagExterior = 0;
                        }

                        // Elementの終了タグで書き出し
                        if (strNType.Equals("EndElement") &&
                            (strLocalName.Equals("AdmArea") || strLocalName.Equals("BldA")) &&
                            intDepth == 1)
                        {
                            // outputのGeoJSONフッタ
                            // Featureごとにカンマ区切りとするが、最後のFeatureにはカンマを付けない⇒そのための処理
                            if (itemCounter > 0)
                                writer.WriteLine(",");

                            // outputのGeoJSONヘッダ
                            writer.WriteLine("\t\t{");
                            writer.WriteLine("\t\t\t\"type\": \"Feature\",");

                            // outputのGeoJSON属性ヘッダ
                            writer.WriteLine("\t\t\t\"properties\": {");

                            // outputのGeoJSON属性
                            int attrCounter = 0;
                            foreach (KeyValuePair<string, string> item in gisAttrTable)
                            {
                                if (attrCounter > 0)
                                {
                                    writer.WriteLine(",");
                                }
                                writer.Write("\t\t\t\t\"" + item.Key + "\": \""
                                    + item.Value + "\"");
                                attrCounter++;
                            }

                            // outputのGeoJSON属性フッタ
                            writer.WriteLine("\n\t\t\t},");


                            // outputのGeoJSON図形ヘッダ
                            writer.WriteLine("\t\t\t\"geometry\": {");

                            // outputのGeoJSONポリゴンヘッダ
                            writer.WriteLine("\t\t\t\t\"type\": \"Polygon\",");
                            writer.WriteLine("\t\t\t\t\"coordinates\": [");
                            //writer.WriteLine("\t\t\t\t\t[");

                            // outputのGeoJSON点列
                            if (posInteriorOutput.Length > 0 && (!posInteriorOutput.Equals("")))
                                writer.WriteLine(posExteriorOutput + posInteriorOutput);
                            else
                                writer.WriteLine(posExteriorOutput);

                            // outputのGeoJSON図形フッタ
                            //writer.WriteLine("\t\t\t\t\t]");
                            writer.WriteLine("\t\t\t\t]");
                            writer.WriteLine("\t\t\t}");

                            // outputのGeoJSONフッタ
                            writer.Write("\t\t}");

                            // Featureごとにカンマ区切りとするが、最後のFeatureにはカンマを付けない⇒そのための処理
                            itemCounter++;
                        }

                        // Elementの終了タグで書き出し
                        if (strNType.Equals("EndElement") &&
                            (strLocalName.Equals("AdmBdry") || strLocalName.Equals("BldL")) &&
                            intDepth == 1)
                        {
                            // outputのGeoJSONフッタ
                            // Featureごとにカンマ区切りとするが、最後のFeatureにはカンマを付けない⇒そのための処理
                            if (itemCounter > 0)
                                writer.WriteLine(",");

                            // outputのGeoJSONヘッダ
                            writer.WriteLine("\t\t{");
                            writer.WriteLine("\t\t\t\"type\": \"Feature\",");

                            // outputのGeoJSON属性ヘッダ
                            writer.WriteLine("\t\t\t\"properties\": {");

                            // outputのGeoJSON属性
                            int attrCounter = 0;
                            foreach (KeyValuePair<string, string> item in gisAttrTable)
                            {
                                if (attrCounter > 0)
                                {
                                    writer.WriteLine(",");
                                }
                                writer.Write("\t\t\t\t\"" + item.Key + "\": \""
                                    + item.Value + "\"");
                                attrCounter++;
                            }
                            //foreach (KeyValuePair<string, string> item in gisAttrTable)
                            //{
                            //    writer.WriteLine("\t\t\t\t\"" + item.Key + "\": \""
                            //        + item.Value + "\"");
                            //}

                            // outputのGeoJSON属性フッタ
                            writer.WriteLine("\n\t\t\t},");


                            // outputのGeoJSON図形ヘッダ
                            writer.WriteLine("\t\t\t\"geometry\": {");

                            // outputのGeoJSONポリゴンヘッダ
                            writer.WriteLine("\t\t\t\t\"type\": \"LineString\",");
                            writer.WriteLine("\t\t\t\t\"coordinates\": [");
                            //writer.WriteLine("\t\t\t\t\t[");

                            // outputのGeoJSON点列
                            if (posInteriorOutput.Length > 0 && (!posInteriorOutput.Equals("")))
                                writer.WriteLine(posExteriorOutput + posInteriorOutput);
                            else
                                writer.WriteLine(posExteriorOutput);

                            // outputのGeoJSON図形フッタ
                            //writer.WriteLine("\t\t\t\t\t]");
                            writer.WriteLine("\t\t\t\t]");
                            writer.WriteLine("\t\t\t}");

                            // outputのGeoJSONフッタ
                            writer.Write("\t\t}");

                            // Featureごとにカンマ区切りとするが、最後のFeatureにはカンマを付けない⇒そのための処理
                            itemCounter++;
                        }

                        // Elementの終了タグで書き出し
                        if (strNType.Equals("EndElement") &&
                            (strLocalName.Equals("AdmPt")) &&
                            intDepth == 1)
                        {
                            // outputのGeoJSONフッタ
                            // Featureごとにカンマ区切りとするが、最後のFeatureにはカンマを付けない⇒そのための処理
                            if (itemCounter > 0)
                                writer.WriteLine(",");

                            // outputのGeoJSONヘッダ
                            writer.WriteLine("\t\t{");
                            writer.WriteLine("\t\t\t\"type\": \"Feature\",");

                            // outputのGeoJSON属性ヘッダ
                            writer.WriteLine("\t\t\t\"properties\": {");

                            // outputのGeoJSON属性
                            int attrCounter = 0;
                            foreach (KeyValuePair<string, string> item in gisAttrTable)
                            {
                                if (attrCounter > 0)
                                {
                                    writer.WriteLine(",");
                                }
                                writer.Write("\t\t\t\t\"" + item.Key + "\": \""
                                    + item.Value + "\"");
                                attrCounter++;
                            }
                            //foreach (KeyValuePair<string, string> item in gisAttrTable)
                            //{
                            //    writer.WriteLine("\t\t\t\t\"" + item.Key + "\": \""
                            //        + item.Value + "\"");
                            //}

                            // outputのGeoJSON属性フッタ
                            writer.WriteLine("\n\t\t\t},");


                            // outputのGeoJSON図形ヘッダ
                            writer.WriteLine("\t\t\t\"geometry\": {");

                            // outputのGeoJSONポリゴンヘッダ
                            writer.WriteLine("\t\t\t\t\"type\": \"Point\",");
                            writer.Write("\t\t\t\t\"coordinates\": ");
                            //writer.WriteLine("\t\t\t\t\"coordinates\": [");
                            //writer.WriteLine("\t\t\t\t\t[");

                            // outputのGeoJSON点列
                            if (posInteriorOutput.Length > 0 && (!posInteriorOutput.Equals("")))
                                writer.WriteLine(posExteriorOutput + posInteriorOutput);
                            else
                                writer.WriteLine(posExteriorOutput);

                            // outputのGeoJSON図形フッタ
                            //writer.WriteLine("\t\t\t\t\t]");
                            //writer.WriteLine("\t\t\t\t]");
                            writer.WriteLine("\t\t\t}");

                            // outputのGeoJSONフッタ
                            writer.Write("\t\t}");

                            // Featureごとにカンマ区切りとするが、最後のFeatureにはカンマを付けない⇒そのための処理
                            itemCounter++;
                        }
                    }

                    // outputのGeoJSONフッタ
                    writer.WriteLine("");
                    writer.WriteLine("\t]");
                    writer.WriteLine("}");
                }
            }
            catch (Exception exc)
            {
                strOutput += "Error: " + exc.Message;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
                if (xmlReader != null)
                {
                    xmlReader.Close();
                }
            }

            Console.WriteLine("Fin.");
        }
    }
}
