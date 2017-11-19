using System;
using System.Collections.Generic;
using System.Text;

namespace MyBlindConsole
{
    class MyClass
    {
        Dictionary<string, string> greetings = new Dictionary<string, string>();
        public MyClass()
        {

        }        

        public string GreeteMe(string name)
        {
            if (greetings.ContainsKey(name))
                return greetings[name];
            else return "You're not in my list";
        }
    }
}
