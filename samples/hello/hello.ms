function main() {
    let name = readLine("Name: ")
    print("Hello " + name + "!")
    let x = string(true)
    let y = int("42")
    let z = any("text")
    let b = any(false)
}

function readLine(prompt: string): string {
    print(prompt)
    let name = input()
    return name
}