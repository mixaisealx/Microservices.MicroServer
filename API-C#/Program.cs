using System;


namespace APIdev {
    class Program {

        static void Main(string[] args) {
            MicroServerAPI.MicroService mserv = new("http://localhost:8080/microserver/get-job", "http://localhost:8080/microserver/post-job", false);
            
            Console.WriteLine("Started");
            string result = mserv.ProcessAsFunction<string, bool>("PyTest.01", true);

            Console.WriteLine(result);
        }
    }
}
