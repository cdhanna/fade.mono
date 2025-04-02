// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using TexTools.Commands;




var root = new RootCommand();

{ // clean scan command
    var command = new Command(
        name: "clean-scan",
        description: "clean up a scanned image"
    );

    var fileArg = new Argument<string>("file", "the png scan file");
    
    command.AddArgument(fileArg);
    command.SetHandler(CleanScanCommand.Run, fileArg);
    
    root.AddCommand(command);
}

root.Invoke(args);