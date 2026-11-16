import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:majstorio_desktop/constants.dart';
import 'package:majstorio_desktop/main.dart';
import 'package:majstorio_desktop/models/auth/auth_session.dart';
import 'package:majstorio_desktop/providers/auth/auth_provider.dart';
import 'package:majstorio_desktop/providers/dashboard/dashboard_provider.dart';
import 'package:provider/provider.dart';

Widget app() {
  return MultiProvider(
    providers: [
      ChangeNotifierProvider(create: (context) => AuthProvider()),
      ChangeNotifierProvider(create: (context) => DashboardProvider()),
    ],
    child: const MyApp(),
  );
}

void main() {
  setUp(AuthSession.clear);

  testWidgets('the app starts on the login screen', (tester) async {
    await tester.pumpWidget(app());
    await tester.pumpAndSettle();

    expect(tester.takeException(), null);
    expect(find.text('Sign in'), findsOneWidget);
    expect(find.text('Welcome, Administrator'), findsNothing);
  });

  testWidgets('empty fields are rejected before any request is sent', (tester) async {
    await tester.pumpWidget(app());
    await tester.tap(find.text('Sign in'));
    await tester.pump();

    expect(find.text('The user name is required'), findsOneWidget);
    expect(find.text('The password is required'), findsOneWidget);
  });

  test('the token is sent only while signed in', () {
    expect(authHeaders().containsKey('Authorization'), false);

    AuthSession.token = 'abc';

    expect(authHeaders()['Authorization'], 'Bearer abc');
    expect(authHeaders()['content-type'], 'application/json');
  });

  testWidgets('signing out returns to the login screen', (tester) async {
    await tester.pumpWidget(app());
    AuthSession.token = 'abc';

    navigatorKey.currentState!.pushNamedAndRemoveUntil(loginRoute, (route) => false);
    await tester.pumpAndSettle();

    expect(find.byType(TextFormField), findsNWidgets(2));
    expect(navigatorKey.currentState!.canPop(), false);
  });
}
