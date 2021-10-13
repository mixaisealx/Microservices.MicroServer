using System;


namespace MicroServer.Debugger {
    static partial class CMDs {

        internal static void DisplayHelp() {
            Console.WriteLine("-- Each command saves the result of its execution to a file in the working directory --");
            Console.WriteLine("exstat - display the list of externals statuses");
            Console.WriteLine("pend - display the list of pending GET connections");
            Console.WriteLine("loctp - display the list of locally available types");
            Console.WriteLine("tystat - display the count of jobs for each type");
            Console.WriteLine("isnap - display the snapshot of local storage");
            Console.WriteLine("gethist - display the list of processed GET-requests");
            Console.WriteLine("posthist - display the list of processed POST-requests");
            Console.WriteLine("flush - remove commands outputs from the previous sessions from log files");
            Console.WriteLine("validate - run server validation test");
            Console.WriteLine("exit - exit from the program");
        }
    }
}
