# Cliente ZariVirusKiller

Este diretório contém o cliente Windows (WinForms .NET 4.8) do ZariVirusKiller.

## Como usar

1. Abra `ZariVirusKiller.sln` no Visual Studio 2019+.
2. Compile em modo Release.
3. Execute `build.bat` para gerar instalador standalone.

## Estrutura

- src/: aplicação WinForms
- ui/: código de UI customizada
- engine/: motor de scan
- key_verification/: validação de licença
- updates/: atualizações de definições
- plugins/: plugins futuros
- data/translations.json: recursos de idioma
