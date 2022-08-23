
namespace KotlinNative2Net;

delegate IntPtr Void_Ptr();

delegate int Ptr_Int(IntPtr inst);

delegate void Ptr_Void(IntPtr inst);

delegate IntPtr IntInt_Ptr(int a, int b);

delegate int PtrPtrIntInt_Int(IntPtr inst, IntPtr f, int a, int b);

