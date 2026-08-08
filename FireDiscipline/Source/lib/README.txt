HƯỚNG DẪN CẤU HÌNH BIÊN DỊCH (BUILD INSTRUCTIONS)
==================================================

Để biên dịch dự án FireDiscipline trên máy của bạn:

CÁCH 1 (Khuyên dùng):
Chạy lệnh dotnet build với tham số đường dẫn tới thư mục Managed của RimWorld:
  dotnet build /p:RimWorldPath="ĐƯỜNG_DẪN_TỚI_RIMWORLD\RimWorldWin64_Data\Managed"

Ví dụ:
  dotnet build /p:RimWorldPath="D:\Games\Rimworld\RimWorldWin64_Data\Managed"

CÁCH 2:
Copy 2 tệp DLL từ thư mục RimWorld:
  1. Assembly-CSharp.dll
  2. UnityEngine.CoreModule.dll
vào thư mục 'Source/lib/' này.
Sau đó mở terminal tại 'Source/FireDiscipline' và chạy:
  dotnet build
