# Threat Model (STRIDE ringan)

Asset utama: stream key, setup token, video lokal, settings, binary MediaMTX, dan firewall rules.

Trust boundary: HP/LAN tidak dipercaya; portal hanya menerima token acak berumur pendek.
API, metrics, RTSP, dan preview MediaMTX dibatasi loopback; secret disimpan dengan DPAPI
CurrentUser pada Windows; binary diverifikasi SHA-256 sebelum dijalankan.

Risiko residual: siapa pun pada LAN yang memperoleh URL RTMP selama masa pakai dapat mencoba
publisher; operator wajib menjaga URL. Wi‑Fi venue/AP isolation dan source overlay DJI tidak
dapat dihilangkan oleh relay remux-only.
