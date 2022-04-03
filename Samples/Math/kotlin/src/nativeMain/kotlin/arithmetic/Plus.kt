package arithmetic

class Plus (private val a: Int, private val b: Int) {
    fun add(): Int {
        return a + b
    }
}

class Minus (private val a: Int, private val b: Int) {
    fun subtract(): Int {
        return a - b
    }
}