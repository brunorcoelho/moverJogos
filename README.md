# 🎮 Game Mover — Playnite Extension

Extensão para o [Playnite](https://playnite.link/) que permite mover jogos instalados entre discos/drives de armazenamento de forma rápida e prática.

## ✨ Funcionalidades

- **Mover jogos entre drives** — Transfira jogos de um disco para outro sem reinstalar.
- **Visualização de espaço em disco** — Veja o espaço livre e usado de cada drive.
- **Integrado ao Playnite** — Acesse diretamente pela sidebar ou pelo menu principal.

## 📦 Instalação

1. Compile o projeto ou baixe a release mais recente.
2. Copie a pasta de saída (`bin/Debug`) para a pasta de extensões do Playnite:
   ```
   %AppData%\Playnite\Extensions\GameMover
   ```
3. Reinicie o Playnite.

## 🚀 Como Usar

1. Abra o Playnite.
2. Clique em **Game Mover** na sidebar ou vá em **Menu → Game Mover → Abrir Game Mover**.
3. Selecione o jogo que deseja mover e o drive de destino.
4. Clique em **Mover** e aguarde a transferência.

## 🛠️ Tecnologias

- **C#** / **.NET Framework 4.6.2**
- **WPF** (interface gráfica)
- **Playnite SDK 6.11.0**

## 📁 Estrutura do Projeto

```
├── GameMoverPlugin.cs    # Plugin principal (sidebar + menu)
├── Models/               # Modelos de dados (GameEntry, DiskInfo)
├── ViewModels/           # Lógica da interface (MVVM)
├── Views/                # Interface XAML
├── extension.yaml        # Metadados da extensão
└── icon.png              # Ícone da extensão
```

## 📝 Licença

Uso pessoal. Desenvolvido por **Bruno**.
