# 🧹 StreamCleaner

**StreamCleaner** é um serviço em background (*Worker Service*) desenvolvido em **.NET** para resolver um problema real de gestão de armazenamento: a proliferação silenciosa de arquivos duplicados em discos de backup.

## 📖 O Problema (Por que este projeto existe?)

Eu possuo uma máquina dedicada exclusivamente para atuar como meu servidor de backups pessoais. Frequentemente, faço o descarregamento de dados de múltiplos aparelhos e fontes — smartphones, computadores de trabalho, pendrives e diretórios do Google Drive — jogando tudo nesse servidor central de forma não estruturada ("bagunçada"). 

O resultado? Gigabytes de espaço desperdiçados com arquivos idênticos copiados de lugares diferentes (ex: a mesma foto vinda do celular e de um backup antigo da nuvem). 

Fazer essa limpeza manualmente é impossível. Fazer via script carregando tudo na RAM trava a máquina. O **StreamCleaner** nasceu para automatizar essa varredura com foco em performance de I/O e segurança dos dados.

## 🚀 Funcionalidades e Arquitetura

O sistema não olha para o nome ou data do arquivo, mas sim para o seu **conteúdo real**. Para garantir que a aplicação seja capaz de analisar Terabytes de arquivos sem estourar a memória RAM, ela utiliza um funil de três etapas:

1. **Busca Resiliente:** Navega por árvores de diretórios ignorando pastas bloqueadas pelo Sistema Operacional (`UnauthorizedAccessException`), evitando quebras na varredura.
2. **Filtro de Metadados (O(1)):** Agrupa arquivos pelo tamanho exato em bytes. Arquivos com tamanhos únicos são imediatamente descartados da análise.
3. **Hashing Assíncrono:** Para os arquivos com o mesmo tamanho, o sistema utiliza `FileStream` com buffers para ler os arquivos em pequenos blocos (chunks) e gera um Hash **SHA-256**, garantindo precisão absoluta sem carregar os arquivos na RAM.

### 🛡️ Segurança e Ações
* **Quarentena (Soft Delete):** Permite mover os arquivos duplicados para uma pasta de backup em vez de excluí-los imediatamente, alterando o nome do arquivo com um sufixo único (GUID) para evitar colisões.
* **Limpeza Automática:** Pode ser configurado para manter apenas o arquivo com a data de modificação mais recente e apagar permanentemente as cópias mais antigas.
* **Relatórios Auditáveis:** Gera relatórios detalhados em `JSON` a cada varredura e logs diários estruturados em `.txt` utilizando o **Serilog**.

---

## 🛠️ Tecnologias Utilizadas

* **.NET 8** (Worker Service)
* **C#** (LINQ, `FileStream`, `Cryptography.SHA256`)
* **Serilog** (File Sinks, Rolling Logs)
* **System.Text.Json**

---

## ⚙️ Como Configurar (`appsettings.json`)

Toda a inteligência do serviço é controlada pelo `appsettings.json`. Você não precisa alterar o código para mudar o comportamento da limpeza.

```json
{
  "ScannerSettings": {
    "RunIntervalMinutes": 60,
    "RemoveOlderFiles": true, 
    "MoveToBackupFolder": "C:\\Quarentena_Duplicados", 
    "TargetDirectories": [
      "D:\\Meus_Backups",
      "E:\\GoogleDrive_BKP"
    ],
    "AllowedExtensions": [],
    "OutputPath": "C:\\Logs_StreamCleaner"
  }
}

## English Version

## 🧹 StreamCleaner

**StreamCleaner** is a background service (*Worker Service*) built in **.NET** to solve a real-world storage management problem: the silent proliferation of duplicate files across backup drives.

## 📖 The Problem (Why does this project exist?)

I have a dedicated machine that acts exclusively as my personal backup server. I frequently dump data from multiple devices and sources—smartphones, work computers, flash drives, and Google Drive folders—into this central server in an unstructured, messy way.

The result? Gigabytes of wasted storage space taken up by identical files copied from different locations (e.g., the same photo downloaded from the cloud and directly from my phone).

Cleaning this up manually is impossible. Doing it via a simple script that loads everything into RAM will crash the machine. **StreamCleaner** was created to automate this scan, focusing heavily on I/O performance and data safety.

## 🚀 Features and Architecture

The system doesn't rely on file names or modification dates; it analyzes the **actual content** of the files. To ensure the application can handle Terabytes of data without running out of RAM, it uses a highly optimized three-step funnel:

1. **Resilient Scanning:** Navigates through deep directory trees while safely ignoring OS-locked folders (`UnauthorizedAccessException`), preventing the scan from crashing.
2. **Metadata Filter (O(1)):** Groups files by their exact size in bytes. Files with unique sizes are instantly discarded from the analysis.
3. **Asynchronous Hashing:** For files that share the exact same size, the system uses `FileStream` with buffers to read them in small chunks and computes a **SHA-256 Hash**. This guarantees absolute accuracy without ever loading the entire file into memory.

### 🛡️ Safety & Actions
* **Quarantine (Soft Delete):** Instead of immediate deletion, it can move duplicate files to a designated backup folder. It automatically appends a unique suffix (GUID) to the filenames to prevent collisions.
* **Automatic Cleanup:** Can be configured to keep only the most recently modified version of a file and permanently delete the older copies.
* **Auditable Reports:** Generates highly detailed `JSON` reports after every scan and maintains daily structured `.txt` logs using **Serilog**.

---

## 🛠️ Built With

* **.NET 8** (Worker Service)
* **C#** (LINQ, `FileStream`, `Cryptography.SHA256`)
* **Serilog** (File Sinks, Rolling Logs)
* **System.Text.Json**

---

## ⚙️ Configuration (`appsettings.json`)

All of the service's intelligence is driven by the `appsettings.json` file. You don't need to touch the code to change how the cleaner behaves.

```json
{
  "ScannerSettings": {
    "RunIntervalMinutes": 60,
    "RemoveOlderFiles": true, 
    "MoveToBackupFolder": "C:\\Quarantine_Duplicates", 
    "TargetDirectories": [
      "D:\\My_Backups",
      "E:\\GoogleDrive_BKP"
    ],
    "AllowedExtensions": [],
    "OutputPath": "C:\\Logs_StreamCleaner"
  }
}
