# TomorrowDAO-server

TomorrowDAO Server provides interface services for the TomorrowDAO pa=latform. In terms of project architecture, the project is developed based on the ABP framework. It uses Orleans, which is a framework for building reliable and scalable distributed applications that can simplify the complexity of distributed computing. In terms of data storage, the project uses Grain and Elasticsearch for data storage and retrieval. Grain is the core component of Orleans and represents an automatically scalable and fault-tolerant entity. In summary, TomorrowDAO Server combines the advantages of the ABP framework, Orleans, and Elasticsearch to achieve a high-performance and scalable distributed wallet interface service.
## Getting Started

Before running TomorrowDAO Server, you need to prepare the following infrastructure components, as they are essential for the project's operation:
* MongoDB
* Elasticsearch
* Redis
* Kafka

The following command will clone TomorrowDAO Server into a folder. Please open a terminal and enter the following command:
```Bash
git clone https://github.com/TomorrowDAOProject/tomorrowDAO-server
```

The next step is to build the project to ensure everything is working correctly. Once everything is built and configuration file is configured correctly, you can run as follows:

```Bash
# enter the tomorrowDAO-server folder
cd tomorrowDAO-server

# publish
dotnet publish src/TomorrowDAOServer.DbMigrator/TomorrowDAOServer.DbMigrator.csproj -o tomorrowDAO/DbMigrator
dotnet publish src/TomorrowDAOServer.AuthServer/TomorrowDAOServer.AuthServer.csproj -o tomorrowDAO/AuthServer
dotnet publish src/TomorrowDAOServer.Silo/TomorrowDAOServer.Silo.csproj -o tomorrowDAO/Silo
dotnet publish src/TomorrowDAOServer.HttpApi.Host/TomorrowDAOServer.HttpApi.Host.csproj -o tomorrowDAO/HttpApi
dotnet publish src/TomorrowDAOServer.EntityEventHandler/TomorrowDAOServer.EntityEventHandler.csproj -o tomorrowDAO/EntityEventHandler

# enter tomorrowDAO folder
cd tomorrowDAO
# ensure that the configuration file is configured correctly

# run DbMigrator service
dotnet DbMigrator/TomorrowDAOServer.DbMigrator.dll

# run AuthServer service
dotnet AuthServer/TomorrowDAOServer.AuthServer.dll

# run Silo service
dotnet Silo/TomorrowDAOServer.Silo.dll

# run HttpApi service
dotnet HttpApi/TomorrowDAOServer.HttpApi.Host.dll

# run EntityEventHandler service
dotnet EntityEventHandler/TomorrowDAOServer.EntityEventHandler.dll
```

After starting all the above services, TomorrowDAO Server is ready to provide external services.

## Modules

TomorrowDAO Server includes the following services:

- `TomorrowDAOServer.DbMigrator`: Data initialization service.
- `TomorrowDAOServer.AuthServer`: Authentication service.
- `TomorrowDAOServer.Silo`: Silo service.
- `TomorrowDAOServer.HttpApi.Host`: API interface service.
- `TomorrowDAOServer.EntityEventHandler`: event handling service.

## Contributing

We welcome contributions to the TomorrowDAO Server project. If you would like to contribute, please fork the repository and submit a pull request with your changes. Before submitting a pull request, please ensure that your code is well-tested.


## License

TomorrowDAO Server is licensed under [MIT](https://github.com/Portkey-Wallet/portkey-DID-server/blob/master/LICENSE).

## Contact

If you have any questions or feedback, please feel free to contact us at the TomorrowDAO community channels. You can find us on Discord, Telegram, and other social media platforms.

Links:

- Website: https://tmrwdao.com/
- Twitter: https://x.com/tmrwdao
- Discord: https://discord.com/invite/gTWkeR5pQB
- Telegram: https://t.me/tmrwdao