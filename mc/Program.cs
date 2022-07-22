// See https://aka.ms/new-console-template for more information
while(true) {
    Console.Write("> ");
    var line = Console.ReadLine();
    if(string.IsNullOrWhiteSpace(line)) {
        return;
    }

    if(line == "1 + 2 * 3") {
        Console.WriteLine("7");
    }
    else {
        Console.WriteLine("ERROR: Unknown expression");
    }
}