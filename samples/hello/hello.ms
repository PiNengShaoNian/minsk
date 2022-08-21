function main() {
    for i = 0 to 10 {
        print(string(random(45)))
    }
}

function readLine(prompt: string): string {
    print(prompt)
    let name = input()
    return name
}