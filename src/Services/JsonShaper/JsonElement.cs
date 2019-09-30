using System.Text;
using System.Collections.Generic;

namespace Services.JsonShaper
{
       public class JsonElement{

       public Dictionary<string, string> Values = new Dictionary<string, string>();

       public Dictionary<string, JsonElement> SubElements = new Dictionary<string, JsonElement>();

       internal JsonElement GetByKey(string[] data){

           var result = this;

           for (var i=0; i<data.Length-1; i++){

               if (!result.SubElements.ContainsKey(data[i]))
                   result.SubElements.Add(data[i], new JsonElement());

                 result = result.SubElements[data[i]];
           }


           return result;



       } 


       private static void AppendSpaces(StringBuilder sb, int level){
           sb.Append(new string(' ', level));
       }

       private static void GetString(JsonElement src, StringBuilder sb, int level){
           AppendSpaces(sb, level);

           sb.Append("{");

           var firstValue = true;


           foreach(var itm in src.Values){

               if (firstValue){
                   sb.Append("\n");
                   firstValue = false;
               }
               else{
                   sb.Append(",\n");
               }

               AppendSpaces(sb, level);
               sb.Append(" \""+itm.Key+"\":"+itm.Value);
           }

           foreach(var itm in src.SubElements){
             if (firstValue){
                   sb.Append("\n");
                   firstValue = false;
               }
               else{
                   sb.Append(",\n");
               }

               sb.Append(" \n");

              AppendSpaces(sb, level); 
              sb.Append(" \""+itm.Key+"\":\n");
              GetString(itm.Value, sb, level+1);
           }

           sb.Append("\n");
           AppendSpaces(sb, level); 
           sb.Append("}");
       }

       public string GetString(){
           var sb = new StringBuilder();
           GetString(this, sb, 0);
           return sb.ToString();
       }
   }



}
