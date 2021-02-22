FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim
ARG USERNAME=vscode
ARG USER_UID=1000
ARG USER_GID=${USER_UID}
ARG compose_version=1.28.4
ENV DEBIAN_FRONTEND=noninteractive

# Install required and misc tools
RUN apt-get update && apt-get -y install --no-install-recommends apt-utils dialog 2>&1 \
    && apt-get -y install openssh-client less iproute2 apt-transport-https gnupg2 curl lsb-release \
    git procps redis-tools ca-certificates vim nano groff zip file jq wget \
    # Create a non-root user to use if preferred - see https://aka.ms/vscode-remote/containers/non-root-user.
    && groupadd --gid $USER_GID $USERNAME \
    && useradd -s /bin/bash --uid $USER_UID --gid $USER_GID -m $USERNAME \
    # [Optional] Add sudo support for the non-root user
    && apt-get install -y sudo \
    && echo $USERNAME ALL=\(root\) NOPASSWD:ALL > /etc/sudoers.d/$USERNAME\
    && chmod 0440 /etc/sudoers.d/$USERNAME


# Install support for docker-in-docker
RUN apt-get install -y apt-transport-https ca-certificates curl gnupg-agent software-properties-common lsb-release \
    && curl -fsSL https://download.docker.com/linux/$(lsb_release -is | tr '[:upper:]' '[:lower:]')/gpg | apt-key add - 2>/dev/null \
    && add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/$(lsb_release -is | tr '[:upper:]' '[:lower:]') $(lsb_release -cs) stable" \
    && apt-get update \
    && apt-get install -y docker-ce-cli \
    #
    # Install Docker Compose
    && curl -sSL "https://github.com/docker/compose/releases/download/${compose_version}/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose \
    && chmod +x /usr/local/bin/docker-compose

# Cleanup APT
RUN apt-get autoremove -y \
    && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/*

# This is needed for Global Tools to work
USER ${USERNAME}
ENV PATH="${PATH}:/home/${USERNAME}/.dotnet/tools"
# Install .NET Global Tools
RUN dotnet tool install -g dotnet-format \
    && dotnet tool install -g microsoft.dotnet-httprepl

USER root
RUN openssl req -x509 -newkey rsa:4096 -keyout /tmp/devcert_key.pem -out /tmp/devcert.pem -days 30 \
    -passin pass:SecurePwdGoesHere -passout pass:SecurePwdGoesHere -subj "/CN=localhost" \
    && openssl pkcs12 -export -out /tmp/devcert.pfx -inkey /tmp/devcert_key.pem -in /tmp/devcert.pem \
    -passin pass:SecurePwdGoesHere -passout pass:SecurePwdGoesHere \
    && openssl pkcs12 -in /tmp/devcert.pfx -clcerts -nokeys -out /tmp/devcert.crt -passin pass:SecurePwdGoesHere \
    && mkdir /certs \
    && cp /tmp/devcert* /certs \
    && chown ${USERNAME} /certs/devcert* \
    && cp /tmp/devcert.crt /usr/local/share/ca-certificates \
    && chmod 755 /usr/local/share/ca-certificates/devcert.crt \
    && update-ca-certificates \
    && rm /tmp/devcert* 

ENV DEBIAN_FRONTEND=dialog

WORKDIR /workspace
