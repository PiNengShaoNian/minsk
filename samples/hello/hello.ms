function main() {
    var name = "something"
    var otherName = "other"

    print("What's your name?")
    otherName = name = input()
    print("Hello " + name + "!")
    print(otherName)
}