nuget restore
msbuild BasicBot.sln -p:DeployOnBuild=true -p:PublishProfile=clario-Web-Deploy.pubxml -p:Password=

