# MessageRPC
一个 用 C# 实现的 使用 Message 的 RPC

MessageRPC ，使用 Message 进行 RPC 通信 。 每一个调用（Call） 就是一次 发送消息（Send）， 返回结果也是返回一条消息 。

MessageRPC 的 RPC 协议格式 可以算是 Http 的 一个 简化版 。 包括了 Head 和 Body（Content） ， Head 包含多个 Header ， 目前定义的 Header 有 3 个： Parameters Error Content-Length ， Parameters 用来传递参数，值的格式是 “id=001&name=小明&” 这样，和 Http 查询字符串 格式一样 。 Error 用来 传递 错误信息 。 如果有 Error Header ， 则 rpc 会抛出 RPCServerException 。

Content-Length 用来表示 Body 的长度，可以用 body 来传递 大二进制数据 。 比如 图片 文件 等 。

Header 之间通过 \r\n 分隔， Head 和 Body 之间也通过 \r\n 分隔 ， 这和 Http 是一样的 。

这 3 个 Header 都是可选， 但最少要有 1 个，不然 服务器端 解析 时会报错并关闭连接 。 Header 的值可以为空，如 “Parameters: ” 。

Header 值 会经过 UrlEncode 编码，具体的说是 Parameters 的参数字符串里的 参数名 和 参数值 会 经过 UrlEncode 编码 ，还有 Error 的 值也会经过 UrlEncode 编码 。 Content-Length 的值是 数字（long），所以不需要 UrlEncode 。

在 通信机制 上，采用了 连接池（SocketPool）的机制 ， 每个 Socket 的 存活期 是 2 分钟，超过 2 分钟 未被使用的 Socket 将被回收（关闭）， 连接池大小没有上限 。 这是为了满足 实时响应性 和 吞吐量 。 就是说，如果 连接池 中的 Socket 不够用， 会创建新的 Socket 。

因为采用了 连接池 ， 所以在 数据通信 上必须严格的准确，具体是指 每次发送的 Body（Content） 长度必须等于 Content-Length 的长度， 小于了必须 通过 SendVacancy() 发送 空字符 \0 来补齐 ， 大于了必须截断 。 也因为此， 在 协议通信 中如果发生错误（异常），则会 关闭连接， 否则 上一次的 Content 可能被当成这一次的 Head， 或者， 这一次的 Head 被当成上一次 的 Content， 并且这种错误只要发生一次，之后很可能就一直错误下去，所以最好的做法就是把连接关闭 。

一般情况，发生上述的 数据传输 的 异常，通常是因为 服务器 网络 问题 或者 受到攻击 。

开发中解决的一些问题 ：

通常服务器不会主动关闭连接，如果意外关闭了连接（比如 Message RPC Host 进程重启或关闭），那么 客户端（SocketPool） 不知道服务器端已关闭连接， 仍然会返回 连接池（SocketPool）中的 Socket 使用，但此时 Socket 已经失效，会抛出 “远程主机强行关闭了一个已有的连接” 异常 。 为了解决这个问题， 在 客户端 向 服务器 Send() 的过程中，在 Send Head 的时候，如果 Socket 抛出异常， 会让 SocketPool 新建一个 Socket 重新 Send Head ， 此时如果 服务器 已经恢复正常监听，则 可以正常 通信 ， 如果 服务器 仍然未恢复正常监听，则 Socket 会抛出异常，此时即按正常流程继续执行。

这样做的原因是，以目前了解的资料来看，客户端 如果要知道 服务器 有没有关闭连接 ， 好像需要发送一个 测试消息 。 正常的情况下每次 Send 之前都发送一个测试消息的话对 性能 比较浪费 。 所以就采用了 上述 的 重试 的 方式 。

我想以前用 Ado.net 的时候，有时候会抛出 “基础连接已关闭 ……” 这样的 异常，是不是跟上述类似的情况。啊哈哈哈

另一个问题是 服务器端 在 客户端访问之后一段时间客户端没有访问的话，服务器端的 CPU 占用率 会 上升到 30%左右，也有可能 70 以上 。 这个问题的原因是，客户端访问之后一段时间客户端没有访问的话 ， 客户端 连接池（Socket Pool）会将 Socket 回收掉（Shutdown() Close()）， 客户端 回收掉 Socket 之后， 服务器 会 Receive() 到 一个 长度为 0 的数据 ， 按照 微软 docs 的说法 ， 当对方主机 “优雅的” 关闭了连接后，己方会收到一个 长度为 0 的数据 。 但在我以前写的一个 Socket 程序里，我记得 客户端（WebClient） 一段时间之后会关闭连接，此时 服务器端 确实收到了 一个 长度为 0 的数据 ， 但当再次 Receive() 的时候，就会抛出异常 “你的本机上的软件关闭了一个已有的连接” （客户端 和 服务器 都在我本机） ， 但现在的情况是，再次 Receive() 仍然会收到一个 一个 长度为 0 的数据 ， 并不会抛出异常 ， 而我的程序逻辑是通过 异常 来判断 客户端 是否关闭连接 ， 如果没有异常则一直循环 Receive() 下去，于是就造成了无限循环 Receive() ， 每次接收到 一个 长度为 0 的数据 ， 这样相当于 “空转” ， 就造成了 CPU 占用率升高 。

那为什么 CPU 占用率有时候是 30% ， 有时候是 70% 呢 ， 这个跟 服务器端 “空转” 的 线程数有关， 如果 客户端 和 服务器端 只建立了一个连接，那么 服务器端也只有 1 个线程在监听， 也就只有 1 个线程 “空转” ， 在多核处理器上， 1 个线程空转最多只会 占用 1 个核的资源，并不会占用 100% 的 CPU 。 所以 30% 是只有 1 个线程空转时的情况 ， 70% 是 1 个以上的线程（Socket）空转的情况 。

对于这个问题，现在的解决办法是如果 Receive 的数据长度为 0 （Socket.Receive()方法的返回值）， 则抛出异常，这样就可以关闭连接了。也避免了空转 。

为什么 印象中 WebClient 将连接关闭后 服务器端的 Socket 第一次 Receive() 得到 长度为 0 的数据 而 第二次就抛出异常呢 ？ 也许 WebClient 在 关闭连接 时 先调用了 Socket.DisConnect() 方法， 这是说 也许，因为没有看过 WebClient 的代码， 而我的程序中是直接调用 Shutdown() Close() 来关闭 Socket 的 。 没有调用 DisConnect() 方法 ， 不知会不会跟 DisConnect() 方法有关系 。 但 服务器端 按上述做法（Receive 的数据长度为 0 则 抛出异常）来做应该也是合理， 因为 服务器端 无法控制 客户端 是以什么方法来关闭连接 。

































