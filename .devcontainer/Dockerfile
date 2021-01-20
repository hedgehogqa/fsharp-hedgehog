FROM mcr.microsoft.com/dotnet/core/sdk:3.1

RUN apt update && apt -y install --no-install-recommends --no-install-suggests \
        bash-completion \
        less \
        nano \
 && apt -y autoremove \
 && apt -y clean \
 && rm -rf /var/lib/apt/lists/*

RUN groupadd -g 1000 dev \
 && useradd -g 1000 -u 1000 -m -s /bin/bash dev \
 && mkdir -p /home/dev/.vscode-server/extensions \
 && mkdir -p /home/dev/.vscode-server/extensionsCache \
 && chown -R dev:dev /home/dev

USER dev
