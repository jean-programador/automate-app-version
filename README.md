## Automação de Geração de Versões para Aplicação Desktop

Este projeto foi desenvolvido para otimizar o processo de geração de versões de uma aplicação desktop, eliminando tarefas manuais e repetitivas que faziam parte do meu dia a dia como desenvolvedor.

### Processos Automatizados

* **Limpeza e Reconstrução:** Realiza o "Clean" e "Rebuild" da solução no Visual Studio, garantindo que a versão gerada esteja atualizada e livre de erros de compilação.
* **Cópia e Compactação:** Copia os arquivos necessários do projeto para uma pasta de destino definida pelo usuário e os compacta em um formato conveniente (por exemplo, .rar) para facilitar o armazenamento e distribuição.
* **Upload para o Google Drive:** Envia automaticamente o arquivo compactado para uma conta específica do Google Drive, proporcionando um backup seguro e acessível de cada versão gerada.

### Adaptabilidade

Embora tenha sido inicialmente criado para uma aplicação específica, o projeto foi desenvolvido de forma modular e genérica, permitindo sua adaptação para outros projetos com relativa facilidade. Basta realizar alguns ajustes nas configurações e nos caminhos dos arquivos para que a automação funcione em diferentes contextos.

**Observação:** Para utilizar este projeto, é necessário ter o Visual Studio instalado e configurado corretamente, além de fornecer as credenciais de acesso à conta do Google Drive. Para a compactação dos arquivos foram utilizados os programas Winrar ou 7Zip, por serem os mais utilizados na empresa, então também é preciso ter um desses programas instalados.

**Contribuições são bem-vindas!** Se você tiver alguma sugestão de melhoria ou encontrar algum problema, sinta-se à vontade para abrir uma issue ou enviar um pull request.

**Espero que este projeto seja útil para você!** 
