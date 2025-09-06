FROM mcr.microsoft.com/dotnet/sdk:8.0

# Install tools
RUN apt-get update && apt-get install -y \
    bash git curl make nano \
    && rm -rf /var/lib/apt/lists/*

# Create dev user
ARG UID=1000
ARG GID=1000
RUN groupadd -g $GID devuser && \
    useradd -u $UID -g $GID -ms /bin/bash devuser && \
    mkdir -p /home/devuser/.cache && \
    touch /home/devuser/.bash_history && \
    chown -R devuser:devuser /home/devuser

USER devuser
WORKDIR /workspace

# Bash history config
RUN echo '\
export HISTSIZE=1000\n\
export HISTFILESIZE=2000\n\
export HISTFILE=/home/devuser/.bash_history\n\
shopt -s histappend\n\
PROMPT_COMMAND="history -a; history -n; $PROMPT_COMMAND"\n\
' >> /home/devuser/.bashrc

# Set .NET environment variables
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

EXPOSE 8080

SHELL ["/bin/bash", "-c"]

CMD ["bash"]