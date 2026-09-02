# Soak dan Performance Test

Jalankan pada Windows laptop target dengan publisher DJI/FFmpeg legal test source:

- [ ] RTMP 1080p30 berjalan minimal 2 jam tanpa crash.
- [ ] CPU aplikasi + MediaMTX dicatat tiap 10 menit (target PRD <8%).
- [ ] RAM dicatat; tidak ada pertumbuhan tak terbatas (target preview <400 MB).
- [ ] Reconnect publisher dan restart engine diuji selama soak.
- [ ] Setelah stop, proses child dan seluruh port dapat digunakan kembali.

Status repository: `Pending Manual Validation`.
