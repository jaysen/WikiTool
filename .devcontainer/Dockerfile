FROM mcr.microsoft.com/devcontainers/dotnet:1-10.0-noble

# The base image ships an apt source for dl.yarnpkg.com (leftover from its
# bundled Node.js setup) whose signing key is expired/unreachable. This
# project doesn't use Yarn, and a broken source here fails `apt-get update`
# for any devcontainer feature that installs packages (e.g. desktop-lite).
RUN find /etc/apt/sources.list.d -iname '*yarn*' -delete
