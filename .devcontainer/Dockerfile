FROM mcr.microsoft.com/vscode/devcontainers/dotnetcore:5.0

RUN wget -qO - https://adoptopenjdk.jfrog.io/adoptopenjdk/api/gpg/key/public | sudo apt-key add - \
    && apt-get update \
    && apt-get dist-upgrade -y \
    && apt-get install software-properties-common -y \
    && add-apt-repository --yes https://adoptopenjdk.jfrog.io/adoptopenjdk/deb/ \
    && apt-get update \
    && apt-get install unzip adoptopenjdk-16-hotspot -y \
    && wget -c https://dl.google.com/go/go1.16.7.linux-amd64.tar.gz -O - | tar -xz -C /usr/local \
    && export PATH=$PATH:/usr/local/go/bin \
    && curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.38.0/install.sh | bash \
    && export NVM_DIR="$HOME/.nvm" \
    && [ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh" \
    && [ -s "$NVM_DIR/bash_completion" ] && \. "$NVM_DIR/bash_completion" \
    && nvm install --lts \
    && nvm use --lts \
    && npm i -g typescript eslint prettier

USER codespace