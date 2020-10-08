using System.Collections;
using System.Collections.Generic;

public class SocketMessage
{
    //private members
    public string type;
    public string note;
    public Dictionary<string, string> meta;

    // Constructor
    public SocketMessage(string type, string note, Dictionary<string, string> meta)
    {
        this.type = type;
        this.note = note;
        this.meta = meta;
    }

    //Convert this to json
    public string ToJson()
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(this);
    }
}
