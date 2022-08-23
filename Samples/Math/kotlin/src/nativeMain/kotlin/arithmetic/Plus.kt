package arithmetic

import kotlinx.cinterop.*
import interop.*

class Plus(private val a: Int, private val b: Int) {
    fun add(): Int {
        return a + b
    }
}

class Minus(private val a: Int, private val b: Int) {
    fun subtract(): Int {
        return a - b
    }
}

class Callback() {
    fun call(f: IntInt_Int, a: Int, b: Int): Int = f(a, b)
}
