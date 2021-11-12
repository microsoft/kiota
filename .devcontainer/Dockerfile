FROM mcr.microsoft.com/vscode/devcontainers/dotnetcore:6.0

# installs tools required for installation of other tools
RUN apt-get update \
    && apt-get dist-upgrade -y \
    && apt-get install wget apt-transport-https software-properties-common -y

# installs java
RUN wget -qO - https://adoptopenjdk.jfrog.io/adoptopenjdk/api/gpg/key/public | sudo apt-key add - \
    && apt-get update \
    && add-apt-repository --yes https://adoptopenjdk.jfrog.io/adoptopenjdk/deb/ \
    && apt-get update \
    && apt-get install unzip adoptopenjdk-16-hotspot -y

# installs golang
RUN wget -c https://dl.google.com/go/go1.17.2.linux-amd64.tar.gz -O - | tar -xz -C /usr/local \
    && export PATH=$PATH:/usr/local/go/bin \
    && go get -v golang.org/x/tools/gopls

# installs node
RUN apt-get install nodejs npm -y \
    && npm i -g typescript eslint prettier

# installs powershell
RUN wget -q https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update \
    && add-apt-repository universe \
    && apt-get install -y powershell

# install ruby
RUN apt-get install ruby-full bundler -y

# install github CLI
RUN apt-key adv --keyserver keyserver.ubuntu.com --recv-key C99B11DEB97541F0 \
    && apt-add-repository https://cli.github.com/packages \
    && apt update \
    && apt install gh -y

# General updates for sanity
RUN apt-get dist-upgrade -y