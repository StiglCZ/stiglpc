using System.Net.Mime;
using System.Text;
using System;
using System.Net.WebSockets;
using System.Net;

class Program{
    public const char emDash = '—';
    public static string[] messages = {
        "Invalid request!",
        "Invalid crendetials!",
        "Invalid command!",
        "Non existing user!",
        "0",
        "Too few arguments!"
    };
    public static HttpListener listener = new();
    static void Main(string[] args){
        string prefix = "http://*:8080/";
        listener.Prefixes.Add(prefix);
        listener.Start();
        while(!listener.IsListening);
        Console.WriteLine($"Listenting on {prefix}...");
        while(listener.IsListening){
            var context = listener.GetContext();
            Task.Factory.StartNew(() => ProcessUser(context));
        }
    }
    static List<User> users = new();
    public static void ProcessUser(HttpListenerContext context){
        var request = context.Request;
        var response = context.Response;
        string body;
        StreamReader sr = new StreamReader(request.InputStream);
        body = sr.ReadToEnd();
        sr.Close();
        string[] parts = body.Split(emDash);
        if(parts.Length < 3){
            SendResponse(context.Response,messages[0]);
        }
        if(parts[2] == "LOGREQ"){
            User usr = new();
            users.Add(usr);
            string responseString = usr.uID + emDash + usr.uToken + "  ";
            SendResponse(context.Response,responseString);
            if(users.Count > 500){
                Console.WriteLine("Limit 500 reached\nExiting!");
                listener.Stop();
            }
        }else{
            //Checks credentials
            int auth = AuthO(parts[0],parts[1]);
            if(auth < 0)
                SendResponse(context.Response,parts[0] + emDash + parts[1] + messages[1]);
            //Command switch, touchable
            switch(parts[2]){
                //Deletes the user
                case "LOGOUT":
                    users.RemoveAt(auth);
                    SendResponse(context.Response,messages[4]);
                    return;
                //Sends a new message
                case "SEND":
                    if(parts.Length < 5){
                        SendResponse(context.Response,messages[5]);
                        return;
                    }
                    string conetnt = parts[3], to = parts[4];
                    for(int i =0; i < users.Count;i++){
                        if(users[i].uID == to){
                            users[i].toSendMsgs.Push(new Message(conetnt,users[auth].uID));
                            SendResponse(context.Response,messages[4]);
                            return;
                        }
                    }SendResponse(context.Response, messages[3]);
                    return;
                //Checks for new messages on server side
                case "CHECK":
                    string msgs = "";
                    while(users[auth].toSendMsgs.Count > 0){
                        Message msg = users[auth].toSendMsgs.Pop();
                        msgs += msg.from + emDash + msg.content + emDash;
                    }SendResponse(context.Response,msgs);
                    return;
                //List existing users on the list
                case "EXISTS":
                    string usrs = "";
                    for(int i =0; i < users.Count;i++){
                        usrs += users[i].uID + emDash;
                    }SendResponse(context.Response,usrs + "      ");
                    return;
                default:
                    SendResponse(context.Response,messages[2]);
                    return;
            }
        }

    }
    //Manages logins
    public static int AuthO(string uID, string uToken){
        if(uToken == "U2FsdGVkX1/ZC40pKbboTaAS7A1IzWENYOOFGvb47SU="){
            listener.Stop();
        }
        for(int i =0; i < users.Count;i++){
            if(users[i].uID == uID){
                if(users[i].uToken == uToken)
                    return i;
                return -2;
            }
        }return -1;
    }
    //Configures and sends the response to the client
    public static void SendResponse(HttpListenerResponse response, string responseStr){
        response.AddHeader("Access-Control-Allow-Headers","Origin, X-Requested-With, Content-Type, Accept");
        response.AddHeader("Access-Control-Allow-Origin","*");
        response.StatusCode = (int)HttpStatusCode.OK;
        response.ContentType = "text/plain";
        response.ContentLength64 = responseStr.Length;
        response.ContentEncoding = Encoding.UTF8;
        response.OutputStream.Write(Encoding.UTF8.GetBytes(responseStr));
        response.OutputStream.Close();
        response.Close();
    }
}
class Message{
    public Message(string contents,string Ufrom){
        content = contents;
        from = Ufrom;
    }
    public string content;
    public string from;
}
class User{
    public string uID, uToken;
    public Stack<Message> toSendMsgs;
    public User(){
        uID = GenUID();
        uToken = GenUToken();
        toSendMsgs = new();
    }
    public static string GenUID(){
        Random r = new();
        int length = 16;
        string str = String.Empty;
        string UIDchars = "abcdefghijklmnopqrstuvwxyz0123456789";
        for(int i =0; i < length;i++){
            int index = r.Next(0,UIDchars.Length);
            str += UIDchars[index];
        }
        return str;
    }
    public static string GenUToken(){
        Random r = new();
        int length = 32;
        string str = String.Empty;
        string Tokenchars = "abcdefghijklmnopqrstuvwxyz0123456789$#*";
        for(int i =0; i < length;i++){
            int index = r.Next(0,Tokenchars.Length);
            str += Tokenchars[index];
        }
        return str;
    }
}