#include "windows.h"
#include "Wlanapi.h"
#include "Objbase.h" // only for StringFromCLSID
#include "stdio.h"

#pragma comment(lib, "Wlanapi.lib")

int wmain()
{
	PDWORD  pdwNegotiatedVersio;
	HANDLE phClientHandle;
	WLAN_INTERFACE_INFO_LIST *ifaceList;

	int rc = WlanOpenHandle(2, NULL, &pdwNegotiatedVersio, &phClientHandle);
	wprintf(L"Error: %d\n", rc);
	rc = WlanEnumInterfaces(phClientHandle, NULL, &ifaceList);
	wprintf(L"Error: %d\n", rc);
	GUID InterfaceGuid = ifaceList->InterfaceInfo[0].InterfaceGuid;

	wchar_t szGuidW[40] = { 0 };
	StringFromGUID2(&InterfaceGuid, szGuidW, 40);
	wprintf(L"iface: %s ~ %b", szGuidW, ifaceList->InterfaceInfo[0].isState);

	WLAN_BSS_LIST *bssListPtr;
	rc = WlanGetNetworkBssList(phClientHandle, &InterfaceGuid, NULL, 3, TRUE, NULL, &bssListPtr); // DOT11_BSS_TYPE.dot11_BSS_type_any
	wprintf(L"Error: %d\n", rc);
	for (int i = 0; i < bssListPtr->dwNumberOfItems; i++)
	{
		wprintf(L"NAME: ");
		for (int j = 0; j < bssListPtr->wlanBssEntries[i].dot11Ssid.uSSIDLength; j++)
		{
			wprintf(L"%c", bssListPtr->wlanBssEntries[i].dot11Ssid.ucSSID[j]);
		}
	}
	// Ugh Search first next time... https://msdn.microsoft.com/en-us/library/windows/desktop/ms706749(v=vs.85).aspx
}
