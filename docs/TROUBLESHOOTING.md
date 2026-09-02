# Troubleshooting

## HP tidak dapat membuka halaman setup

Pastikan HP dan laptop berada pada router yang sama, jaringan Windows berstatus Private,
dan firewall rule aplikasi telah diperbaiki. Wi‑Fi venue dapat memakai AP isolation;
gunakan router/hotspot pribadi.

## DJI Fly tidak tersambung

Salin ulang URL RTMP dari slot aktif. Jangan memakai URL RTSP vMix pada DJI Fly.
Pastikan Custom RTMP benar-benar dimulai dan stream key tidak berubah.

## Status tetap menunggu

Periksa jaringan, sinyal, dan status siaran DJI Fly. Jangan mengganti port saat live.
Jika IP laptop berubah, buat URL setup baru.

## vMix tidak menampilkan video

Gunakan URL RTSP loopback dari aplikasi, pilih RTSP over TCP bila tersedia, dan pastikan
status `Video masuk` sebelum melakukan probe ulang.

## Mesin engine gagal mulai

Jalankan Diagnostik dan periksa integritas MediaMTX, konflik port, dan firewall.
Jangan mematikan process lain secara otomatis.

## Preview WebView2 tidak tampil

Pastikan WebView2 Runtime terpasang, status slot sudah `Video masuk`, dan MediaMTX WebRTC loopback aktif. Output RTSP vMix tetap dapat digunakan bila preview gagal.

## Support bundle

Buka Diagnostik, tinjau isi yang sudah di-redact, lalu export. Bundle tidak boleh berisi stream key, token portal, clipboard, atau data pribadi.
