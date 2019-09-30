using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Core.Azure.Blob;
using Newtonsoft.Json;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var SrcJsonConnstring = "DefaultEndpointsProtocol=https;AccountName=lkedevmain;AccountKey=l0W0CaoNiRZQIqJ536sIScSV5fUuQmPYRQYohj/UjO7+ZVdpUiEsRLtQMxD+1szNuAeJ351ndkOsdWFzWBXmdw==;";

            var DestConnString = "";

            var nameBuilder = new NameBuilder();

            var json = new AzureBlobStorage(SrcJsonConnstring).GetAsTextAsync("settings","globalsettings.json").Result;

            Console.WriteLine();
            Console.WriteLine(json);

            Console.WriteLine();

            using (var reader = new JsonTextReader(new StringReader(json)))
            {

                while (reader.Read())
                {

                    if (reader.TokenType == JsonToken.PropertyName)
                        nameBuilder.Add(reader.Value.ToString());

                    if (reader.TokenType == JsonToken.String)
                        WriteToDb(nameBuilder, '"' + reader.Value.ToString() + '"');

                    if (reader.TokenType == JsonToken.Boolean)
                        WriteToDb(nameBuilder, reader.Value.ToString());
                    
                    if (reader.TokenType == JsonToken.Integer)
                        WriteToDb(nameBuilder, reader.Value.ToString());

                    if (reader.TokenType == JsonToken.EndObject)
                        nameBuilder.RemoveLast();                        


                    if (reader.TokenType == JsonToken.StartArray)
                        Console.WriteLine("----------- Start Array -----------");

                    if (reader.TokenType == JsonToken.EndArray)
                        Console.WriteLine("----------- End Array -----------");                    



                }
            }






        }

        private static void WriteToDb(NameBuilder nb, string value)
        {
            Console.WriteLine("{0} : {1}", nb.ToString(), value);

            nb.RemoveLast();
        }
    }


    public class NameBuilder{


        private Dictionary<string, int> _arrays = new Dictionary<string, int>();

        public int GetArrayNo(string fieldName){

            if (!_arrays.ContainsKey(fieldName))
                _arrays.Add(fieldName, 0);

            return _arrays[fieldName]++;
        }


        private List<string> _names = new List<string>();

        public void Add(string name){
            _names.Add(name);
        }

        public void RemoveLast(){
            if (_names.Count == 0)
                return;

            _names.RemoveAt(_names.Count - 1);
        }
        


        public override string ToString(){
            var sb = new StringBuilder();


            foreach(var item in _names){
                if (sb.Length >0)
                    sb.Append('.');

                sb.Append(item);


            }

            return sb.ToString();

        }

    }

}
