# Kurulum Paketi Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** KantarPro'nun gercek kantar bilgisayarlarina kopyalanabilir ve tekrar uretilebilir Release kurulum paketini hazirlamak.

**Architecture:** Paket uretimi gelistirme bilgisayarinda PowerShell scripti ile yapilir. Program ayarlari pakete gomulmez; SQL/IP/COM/yazici ayarlari her bilgisayarda yerel olarak saklanir.

**Tech Stack:** .NET Framework 4.8 WPF, MSBuild, PowerShell, SQL Server Express 2008+.

---

### Task 1: SQL 2008 uyumlu kurulum scriptleri

**Files:**
- Modify: `tools/SetupServerNetwork.ps1`
- Modify: `tools/SetupClientNetwork.ps1`

- [x] SQL instance registry yolu `MSSQL16.SQLEXPRESS` sabitinden kurtarildi.
- [x] SQL Server 2008 uyumlu `sp_addrolemember` kullanildi.
- [x] Client scriptinde `param` blogu ilk statement haline getirildi.

### Task 2: Veritabani scriptlerini temiz kurulum icin tamamla

**Files:**
- Modify: `database/001_create_schema.sql`
- Create: `database/008_add_cikis_no.sql`

- [x] Temiz kurulumda `Islemler.CikisNo` kolonu eklendi.
- [x] Eski veritabanlari icin geriye uyumlu `008_add_cikis_no.sql` eklendi.

### Task 3: Paket uretme scripti

**Files:**
- Create: `tools/BuildReleasePackage.ps1`

- [x] Release build alir.
- [x] `Program`, `Kurulum`, `Veritabani`, `Dokumanlar` klasorlerini olusturur.
- [x] Program exe/dll/config dosyalarini kopyalar.
- [x] Kurulum scriptleri ve dokumanlari pakete ekler.
- [x] ZIP paketi uretir.

### Task 4: Gercek saha kurulum dokumani

**Files:**
- Create: `docs/GERCEK_KANTAR_KURULUM_REHBERI.md`

- [x] Windows 10 server, Windows 7 client ve SQL Server 2008 Express senaryosu belgelendi.
- [x] Farkli IP/COM/yazici ihtimali belgelendi.
- [x] Guncelleme ve prova adimlari yazildi.
