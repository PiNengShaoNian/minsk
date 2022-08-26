function main() {
    var a = "1"
    var b = "2"
    var c = "3"
    var d = "4"
    var e = a + b
    var f = a + b + c
    var g = a + b + c + d
    var h = a + b + c + d + "5"
    print(h)
}

function readLine(prompt: string): string {
    print(prompt)
    let name = input()
    return name
}