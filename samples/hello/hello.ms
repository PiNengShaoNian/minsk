function main() {
    let name = readLine("Name: ")
    print("Hello " + name + "!")
    let x = -1
    let y = +1
    let z = ~1
    let a = true
    let b = !false
    let c = !a
}

function readLine(prompt: string): string {
    print(prompt)
    let name = input()
    return name
}