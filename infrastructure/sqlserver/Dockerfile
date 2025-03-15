FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

RUN mkdir -p /usr/src/app
WORKDIR /usr/src/app

COPY ./create-database.sql /usr/src/app
COPY ./entrypoint.sh /usr/src/app
COPY ./run-initialization.sh /usr/src/app

RUN chmod +x /usr/src/app/run-initialization.sh

CMD /bin/bash ./entrypoint.sh