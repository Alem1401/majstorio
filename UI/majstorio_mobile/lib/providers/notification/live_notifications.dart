import 'dart:async';

import 'package:majstorio_mobile/constants.dart';
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:signalr_netcore/signalr_client.dart';

// Keeps a SignalR connection to the API open and calls onNews when there may be new notifications:
// when the server says one was created, and after every (re)connect, to load the ones missed meanwhile.
class LiveNotifications {
  LiveNotifications(this.onNews);

  final void Function() onNews;

  HubConnection? _connection;
  Timer? _reconnectTimer;

  void start() {
    var connection = HubConnectionBuilder()
        .withUrl(
          "$baseUrl/hubs/notifications",
          options: HttpConnectionOptions(accessTokenFactory: () async => AuthSession.token ?? ""),
        )
        .build();
    // The name must match the one the API sends.
    connection.on("notificationCreated", (_) => onNews());
    _connection = connection;

    _connect();
    // If the connection could not be made or was lost, try again.
    _reconnectTimer = Timer.periodic(const Duration(seconds: 5), (_) => _connect());
  }

  Future<void> _connect() async {
    if (_connection?.state != HubConnectionState.Disconnected) return;
    try {
      await _connection?.start();
      onNews();
    } catch (e) {
      // The API cannot be reached right now; the timer tries again.
    }
  }

  void stop() {
    _reconnectTimer?.cancel();
    _connection?.stop();
  }
}
