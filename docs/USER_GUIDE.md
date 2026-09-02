# Panduan Pengguna

## Status produk saat ini

Build yang tersedia sebelum Phase 0–6 lengkap adalah prerelease untuk development
dan validasi. UI operator penuh, portal QR, preview, dan hardware DJI belum selesai.

## Requirement

- Windows 10 22H2 x64 atau Windows 11 x64.
- Laptop dan HP remote DJI berada pada router/Wi-Fi yang sama.
- DJI Fly mendukung Custom RTMP.
- vMix mendukung RTSP input.

## Instalasi prerelease

1. Buka halaman GitHub Releases dan pilih release Alpha/Beta/RC yang diinginkan.
2. Unduh installer `.exe` dan `SHA256SUMS.txt` dari release yang sama.
3. Jalankan `Get-FileHash <nama-installer>.exe -Algorithm SHA256` di PowerShell.
4. Pastikan hash sama dengan isi `SHA256SUMS.txt`.
5. Jalankan installer. Jika build belum ditandatangani, Windows dapat menampilkan
   peringatan SmartScreen; jangan lanjut bila checksum tidak cocok.

## Alur penggunaan target

1. Hubungkan laptop dan HP ke jaringan lokal yang sama.
2. Jalankan Al Ikhsan Media (Drone Version) dan tunggu penerima video siap.
3. Salin URL RTMP slot ke Custom RTMP pada DJI Fly, lalu mulai siaran.
4. Tunggu status video masuk.
5. Salin URL RTSP loopback ke input Stream/RTSP di vMix.
6. Biarkan aplikasi berjalan selama input drone digunakan.

## Batas saat ini

- Prerelease awal belum mempunyai seluruh UI untuk menjalankan alur di atas.
- Aplikasi tidak melakukan transcoding dan tidak dapat menghapus overlay yang sudah
  tertanam oleh DJI Fly atau firmware.
- Hardware DJI dan vMix aktual belum divalidasi.
- Wi-Fi venue dapat memblokir komunikasi antarperangkat.

## Uninstall

Gunakan **Settings → Apps → Installed apps** pada Windows. Validasi cleanup final,
termasuk firewall rules dan settings, masih merupakan gate Phase 6.
