using Mirage;
using Mirage.Collections;

namespace SyncListTests.SyncListErrorForGenericStruct
{
    class SyncListErrorForGenericStruct : NetworkBehaviour
    {
        MyGenericStructList harpseals;


        struct MyGenericStruct<T>
        {
            T genericpotato;
        }

        class MyGenericStructList : SyncList<MyGenericStruct<float>> { };
    }
}
