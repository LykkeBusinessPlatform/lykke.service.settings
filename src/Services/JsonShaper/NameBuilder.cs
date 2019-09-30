using System.Collections.Generic;
using System.Text;

namespace Services.JsonShaper{

        public class JsonNameChecker{


        private readonly Dictionary<string, int> _arrays = new Dictionary<string, int>();

        public int GetArrayNo(string fieldName){

            if (!_arrays.ContainsKey(fieldName))
                _arrays.Add(fieldName, 0);

            return _arrays[fieldName]++;
        }


        private readonly List<string> _names = new List<string>();

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