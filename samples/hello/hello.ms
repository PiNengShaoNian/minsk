function main() {
    let x = 1
    let y = 2
    let a = x == y
    let b = x != y
    let c = x + y
    let d = x - y
    let e =  x * y
    let f = x / y
    let g = x & y
    let h = x | y
    let i = "1" == "2"
    let j = "1" != "2"

    print(string(i))
    print(string(j))
}

function readLine(prompt: string): string {
    print(prompt)
    let name = input()
    return name
}