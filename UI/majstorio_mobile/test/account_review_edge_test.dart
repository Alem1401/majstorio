
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:majstorio_mobile/constants.dart';
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:majstorio_mobile/providers/account/account_provider.dart';
import 'package:majstorio_mobile/providers/appointment/appointment_provider.dart';
import 'package:majstorio_mobile/providers/auth/auth_provider.dart';
import 'package:majstorio_mobile/providers/base_provider.dart';
import 'package:majstorio_mobile/providers/category/category_provider.dart';
import 'package:majstorio_mobile/providers/city/city_provider.dart';
import 'package:majstorio_mobile/providers/news_post/news_provider.dart';
import 'package:majstorio_mobile/providers/offer/offer_provider.dart';
import 'package:majstorio_mobile/providers/service_request/service_request_provider.dart';
import 'package:majstorio_mobile/providers/tradesperson_profile/tradesperson_profile_provider.dart';
import 'package:majstorio_mobile/screens/account/account_screen.dart';
import 'package:majstorio_mobile/screens/account/profile_edit_screen.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/validators.dart' as validate;
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:provider/provider.dart';

import 'account_and_review_test.dart' show profileJson;
import 'screens_test.dart' show app, signInAs;
import 'test_data.dart';

// Like app() in screens_test.dart, with the login route and the navigator key, so sign-out can navigate.
Widget appWithLogin(Widget screen) {
  return MultiProvider(
    providers: [
      ChangeNotifierProvider(create: (ctx) => AuthProvider()),
      ChangeNotifierProvider(create: (ctx) => CityProvider()),
      ChangeNotifierProvider(create: (ctx) => CategoryProvider()),
      ChangeNotifierProvider(create: (ctx) => NewsProvider()),
      ChangeNotifierProvider(create: (ctx) => TradespersonProfileProvider()),
      ChangeNotifierProvider(create: (ctx) => ServiceRequestProvider()),
      ChangeNotifierProvider(create: (ctx) => OfferProvider()),
      ChangeNotifierProvider(create: (ctx) => AppointmentProvider()),
      ChangeNotifierProvider(create: (ctx) => AccountProvider()),
    ],
    child: MaterialApp(
      navigatorKey: navigatorKey,
      theme: AppTheme.light,
      home: screen,
      routes: {loginRoute: (context) => const Scaffold(body: Text('LOGIN SCREEN'))},
    ),
  );
}

// A host screen that opens "screen" and remembers what it popped with.
class _Host extends StatefulWidget {
  const _Host(this.screen);
  final Widget screen;
  @override
  State<_Host> createState() => _HostState();
}

class _HostState extends State<_Host> {
  Object? popped = 'nothing yet';
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Column(children: [
        ElevatedButton(
          onPressed: () async {
            var result = await Navigator.push(context, MaterialPageRoute(builder: (context) => widget.screen));
            setState(() => popped = result);
          },
          child: const Text('OPEN'),
        ),
        Text('popped: $popped'),
      ]),
    );
  }
}

Finder fieldWithLabel(String label) => find.widgetWithText(TextFormField, label);

Future<void> tapSave(WidgetTester tester) async {
  await tester.ensureVisible(find.text('Spremi'));
  await tester.tap(find.text('Spremi'));
  await tester.pumpAndSettle();
}

void main() {
  setUp(() {
    AuthSession.clear();
  });

  // ---------------- profile edit ----------------

  group('profile edit', () {
    testWidgets('409 on save shows the message under the e-mail field', (tester) async {
      signInAs(AuthSession.customer);
      FakeApi((request) {
        if (request.method == 'PUT') return jsonResponse({'message': 'This email is already in use.'}, 409);
        return jsonResponse(profileJson());
      });
      await tester.pumpWidget(app(const ProfileEditScreen()));
      await tester.pumpAndSettle();

      await tester.enterText(fieldWithLabel('E-pošta'), 'zauzeto@majstorio.test');
      await tapSave(tester);

      expect(find.descendant(of: fieldWithLabel('E-pošta'), matching: find.text('This email is already in use.')),
          findsOneWidget);
      expect(find.byType(ProfileEditScreen), findsOneWidget);
    });

    testWidgets('server field errors show under their fields', (tester) async {
      signInAs(AuthSession.customer);
      FakeApi((request) {
        if (request.method == 'PUT') {
          return jsonResponse({
            'errors': {
              'Email': ['Email must be a valid email address.'],
              'PhoneNumber': ['Phone number is wrong.'],
            },
          }, 400);
        }
        return jsonResponse(profileJson());
      });
      await tester.pumpWidget(app(const ProfileEditScreen()));
      await tester.pumpAndSettle();

      await tapSave(tester);

      expect(find.descendant(of: fieldWithLabel('E-pošta'), matching: find.text('Email must be a valid email address.')),
          findsOneWidget);
      expect(find.descendant(of: fieldWithLabel('Telefon (nije obavezno)'), matching: find.text('Phone number is wrong.')),
          findsOneWidget);
    });

    testWidgets('a server error disappears once the form is saved again successfully', (tester) async {
      signInAs(AuthSession.customer);
      var first = true;
      FakeApi((request) {
        if (request.method == 'PUT') {
          if (first) {
            first = false;
            return jsonResponse({'message': 'This email is already in use.'}, 409);
          }
          return http.Response('', 200);
        }
        return jsonResponse(profileJson());
      });
      await tester.pumpWidget(app(_Host(const ProfileEditScreen())));
      await tester.tap(find.text('OPEN'));
      await tester.pumpAndSettle();

      await tapSave(tester);
      expect(find.descendant(of: fieldWithLabel('E-pošta'), matching: find.text('This email is already in use.')),
          findsOneWidget);
      await tapSave(tester);

      expect(find.byType(ProfileEditScreen), findsNothing);
      expect(find.text('popped: true'), findsOneWidget);
    });

    testWidgets('phone with brackets is rejected without calling the API', (tester) async {
      signInAs(AuthSession.customer);
      var api = FakeApi((request) => jsonResponse(profileJson()));
      await tester.pumpWidget(app(const ProfileEditScreen()));
      await tester.pumpAndSettle();

      await tester.enterText(fieldWithLabel('Telefon (nije obavezno)'), '(061) 123 456');
      await tapSave(tester);

      expect(find.textContaining('Unesite 6 do 20 cifara'), findsOneWidget);
      expect(api.calls.where((call) => call.method == 'PUT'), isEmpty);
    });

    testWidgets('an emptied phone is sent as null and names are trimmed', (tester) async {
      signInAs(AuthSession.customer);
      var api = FakeApi((request) {
        if (request.method == 'PUT') return http.Response('', 200);
        return jsonResponse({...profileJson(), 'phoneNumber': '061 111 222'});
      });
      await tester.pumpWidget(app(const ProfileEditScreen()));
      await tester.pumpAndSettle();
      expect(find.text('061 111 222'), findsOneWidget);

      await tester.enterText(fieldWithLabel('Telefon (nije obavezno)'), '   ');
      await tester.enterText(fieldWithLabel('Ime'), '  Amra  ');
      await tapSave(tester);

      var put = api.calls.singleWhere((call) => call.method == 'PUT');
      expect(put.body, contains('"phoneNumber":null'));
      expect(put.body, contains('"firstName":"Amra"'));
    });

    testWidgets('a load error shows retry, and retry fills the form', (tester) async {
      signInAs(AuthSession.customer);
      var fail = true;
      FakeApi((request) => fail ? jsonResponse({'message': 'Server pao.'}, 500) : jsonResponse(profileJson()));
      await tester.pumpWidget(app(const ProfileEditScreen()));
      await tester.pumpAndSettle();

      expect(find.text('Server pao.'), findsOneWidget);
      expect(find.text('Spremi'), findsNothing);
      fail = false;
      await tester.tap(find.text('Pokušaj ponovo'));
      await tester.pumpAndSettle();

      expect(find.text('Amra'), findsOneWidget);
      expect(find.text('Spremi'), findsOneWidget);
    });

    testWidgets('"Ukloni" asks first; "Ne" keeps the photo, "Da" deletes it', (tester) async {
      signInAs(AuthSession.customer);
      var hasPhoto = true;
      var api = FakeApi((request) {
        if (request.method == 'DELETE') {
          hasPhoto = false;
          return http.Response('', 200);
        }
        return jsonResponse({...profileJson(), 'profileImageUrl': hasPhoto ? 'Account/photos/x.png' : null});
      });
      await tester.pumpWidget(app(const ProfileEditScreen()));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Ukloni'));
      await tester.pumpAndSettle();
      expect(find.text('Ukloniti profilnu fotografiju?'), findsOneWidget);
      await tester.tap(find.text('Ne'));
      await tester.pumpAndSettle();
      expect(api.calls.where((call) => call.method == 'DELETE'), isEmpty);

      await tester.tap(find.text('Ukloni'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Da'));
      await tester.pumpAndSettle();

      var delete = api.calls.singleWhere((call) => call.method == 'DELETE');
      expect(delete.url.path, '/Account/photo');
      expect(find.text('Ukloni'), findsNothing);
      expect(find.text('Dodaj fotografiju'), findsOneWidget);
    });

    testWidgets('typed text survives a photo removal', (tester) async {
      signInAs(AuthSession.customer);
      FakeApi((request) {
        if (request.method == 'DELETE') return http.Response('', 200);
        return jsonResponse({...profileJson(), 'profileImageUrl': 'Account/photos/x.png'});
      });
      await tester.pumpWidget(app(const ProfileEditScreen()));
      await tester.pumpAndSettle();

      await tester.enterText(fieldWithLabel('Prezime'), 'Novo');
      await tester.tap(find.text('Ukloni'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Da'));
      await tester.pumpAndSettle();

      expect(find.text('Novo'), findsOneWidget);
    });

    testWidgets('no "Ukloni" when there is no photo (null or empty)', (tester) async {
      signInAs(AuthSession.customer);
      FakeApi((request) => jsonResponse({...profileJson(), 'profileImageUrl': ''}));
      await tester.pumpWidget(app(const ProfileEditScreen()));
      await tester.pumpAndSettle();

      expect(find.text('Ukloni'), findsNothing);
      expect(find.text('Dodaj fotografiju'), findsOneWidget);
    });
  });

  // ---------------- account screen ----------------

  group('account screen', () {
    testWidgets('header shows the full name from /Account', (tester) async {
      signInAs(AuthSession.customer);
      FakeApi((request) => jsonResponse(profileJson()));
      await tester.pumpWidget(appWithLogin(const AccountScreen()));
      await tester.pumpAndSettle();

      expect(find.text('Amra Hadžić'), findsOneWidget);
      expect(find.text('mobile · Korisnik'), findsOneWidget);
    });

    testWidgets('header falls back to the session user name when /Account fails', (tester) async {
      signInAs(AuthSession.tradesperson);
      FakeApi((request) => jsonResponse({'message': 'x'}, 500));
      await tester.pumpWidget(appWithLogin(const AccountScreen()));
      await tester.pumpAndSettle();

      expect(find.text('test'), findsOneWidget);
      expect(find.text('test · Majstor'), findsOneWidget);
    });

    testWidgets('logout: "Ne" keeps the session and sends nothing', (tester) async {
      signInAs(AuthSession.customer);
      var api = FakeApi((request) => jsonResponse(profileJson()));
      await tester.pumpWidget(appWithLogin(const AccountScreen()));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(find.text('Odjava'), 200, scrollable: find.byType(Scrollable).first);
      await tester.tap(find.text('Odjava'));
      await tester.pumpAndSettle();
      expect(find.text('Da li se želite odjaviti?'), findsOneWidget);
      await tester.tap(find.text('Ne'));
      await tester.pumpAndSettle();

      expect(api.calls.where((call) => call.url.path == '/Auth/logout'), isEmpty);
      expect(AuthSession.token, 'abc');
      expect(find.text('LOGIN SCREEN'), findsNothing);
    });

    for (var scenario in [
      (name: 'server confirms', status: 200, warns: false),
      (name: 'token already invalid (401)', status: 401, warns: false),
      (name: 'server error (500)', status: 500, warns: true),
    ]) {
      testWidgets('logout "Da" when ${scenario.name}: POST, session cleared, login shown', (tester) async {
        signInAs(AuthSession.customer);
        var api = FakeApi((request) {
          if (request.url.path == '/Auth/logout') return http.Response('', scenario.status);
          return jsonResponse(profileJson());
        });
        await tester.pumpWidget(appWithLogin(const AccountScreen()));
        await tester.pumpAndSettle();

        await tester.scrollUntilVisible(find.text('Odjava'), 200, scrollable: find.byType(Scrollable).first);
        await tester.tap(find.text('Odjava'));
        await tester.pumpAndSettle();
        await tester.tap(find.text('Da'));
        await tester.pumpAndSettle();

        var logout = api.calls.singleWhere((call) => call.url.path == '/Auth/logout');
        expect(logout.method, 'POST');
        expect(logout.headers['Authorization'], 'Bearer abc');
        expect(AuthSession.token, isNull);
        expect(AuthSession.userId, isNull);
        expect(find.text('LOGIN SCREEN'), findsOneWidget);
        expect(find.byType(AccountScreen), findsNothing);
        expect(find.textContaining('server nije potvrdio'), scenario.warns ? findsOneWidget : findsNothing);
      });
    }

    testWidgets('logout with the server unreachable still clears the session and warns', (tester) async {
      signInAs(AuthSession.customer);
      BaseProvider.client = MockClient((request) async {
        if (request.url.path == '/Auth/logout') throw const SocketExceptionLike();
        return jsonResponse(profileJson());
      });
      await tester.pumpWidget(appWithLogin(const AccountScreen()));
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(find.text('Odjava'), 200, scrollable: find.byType(Scrollable).first);
      await tester.tap(find.text('Odjava'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Da'));
      await tester.pumpAndSettle();

      expect(AuthSession.isLoggedIn, isFalse);
      expect(find.text('LOGIN SCREEN'), findsOneWidget);
      expect(find.text('Odjavljeni ste na uređaju, ali server nije potvrdio odjavu.'), findsOneWidget);
    });
  });

  // ---------------- providers ----------------

  group('providers', () {
    TestWidgetsFlutterBinding.ensureInitialized();

    test('AuthProvider.logout: 200 and 401 confirm, 500 and no connection do not; session always cleared', () async {
      for (var (status, expected) in [(200, true), (401, true), (500, false), (403, false), (null, false)]) {
        signInAs(AuthSession.customer);
        BaseProvider.client = MockClient((request) async {
          if (status == null) throw const SocketExceptionLike();
          return http.Response('', status);
        });
        expect(await AuthProvider().logout(), expected, reason: 'status $status');
        expect(AuthSession.isLoggedIn, isFalse, reason: 'status $status');
      }
    });

    test('AccountProvider.deletePhoto is a DELETE with the token', () async {
      AuthSession.token = 'abc';
      var api = FakeApi((request) => http.Response('', 200));

      await AccountProvider().deletePhoto();

      expect(api.calls.single.method, 'DELETE');
      expect(api.calls.single.url.path, '/Account/photo');
      expect(api.calls.single.headers['Authorization'], 'Bearer abc');
    });

    test('AccountProvider.uploadPhoto turns a 400 into the server message', () async {
      AuthSession.token = 'abc';
      FakeApi((request) => jsonResponse({'message': 'Only JPEG and PNG photos are allowed.'}, 400));

      expect(
        () => AccountProvider().uploadPhoto([1, 2, 3], 'a.png'),
        throwsA(predicate((e) => e.toString() == 'Only JPEG and PNG photos are allowed.')),
      );
    });
  });

  // ---------------- NetworkPicture ----------------

  group('NetworkPicture', () {
    NetworkImage imageOf(WidgetTester tester) => tester.widget<Image>(find.byType(Image)).image as NetworkImage;

    testWidgets('a relative API address gets the base URL and the token, without content-type', (tester) async {
      AuthSession.token = 'abc';
      await tester.pumpWidget(const MaterialApp(home: NetworkPicture(url: 'Account/photos/0123456789abcdef0123456789abcdef.png')));

      var image = imageOf(tester);
      expect(image.url, '$baseUrl/Account/photos/0123456789abcdef0123456789abcdef.png');
      expect(image.headers, {'Authorization': 'Bearer abc'});
    });

    testWidgets('an absolute address is used as it is, without the token', (tester) async {
      AuthSession.token = 'abc';
      await tester.pumpWidget(const MaterialApp(home: NetworkPicture(url: 'https://cdn.example.test/a.jpg')));

      var image = imageOf(tester);
      expect(image.url, 'https://cdn.example.test/a.jpg');
      expect(image.headers, isNull);
    });

    testWidgets('an http (not https) address is also left untouched', (tester) async {
      AuthSession.token = 'abc';
      await tester.pumpWidget(const MaterialApp(home: NetworkPicture(url: 'http://example.test/a.jpg')));

      expect(imageOf(tester).url, 'http://example.test/a.jpg');
      expect(imageOf(tester).headers, isNull);
    });

    testWidgets('no address shows the placeholder icon and no image', (tester) async {
      for (var url in [null, '']) {
        await tester.pumpWidget(MaterialApp(home: NetworkPicture(url: url)));
        expect(find.byType(Image), findsNothing);
        expect(find.byIcon(Icons.person_outline), findsOneWidget);
      }
    });
  });

  // ---------------- phone rule = server rule ----------------

  group('validators.phone matches the server', () {
    // The server's MyProfileUpdateRequestValidator rule, written out here as the reference:
    // allowed characters, at most 32 characters, 6 to 20 digits (separators not counted).
    bool server(String text) =>
        text.length <= 32 &&
        RegExp(r'^\+?[0-9 /-]+$').hasMatch(text) &&
        RegExp(r'[0-9]').allMatches(text).length >= 6 &&
        RegExp(r'[0-9]').allMatches(text).length <= 20;
    const table = <String, bool>{
      '+387 61 123 456': true,
      '061/123-456': true,
      '123456': true,
      '+123456': true,
      '12345': false,
      '12345678901234567890': true,
      '123456789012345678901': false,
      '+12345678901234567890': true,
      '(061) 123 456': false,
      '061.123.456': false,
      '++38761123456': false,
      '387+61123456': false,
      '061 123 456 ext 2': false,
      '٠٦١١٢٣٤٥٦': false,
      '': true,
      '   ': true,
      '  061 123 456  ': true,
      '------': false,
      '+ / - /': false,
      '00 387 / 61 - 123 - 456': true,
      '1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7': false,
    };

    for (var entry in table.entries) {
      test('"${entry.key}" -> ${entry.value ? "valid" : "invalid"}', () {
        expect(validate.phone(entry.key) == null, entry.value);
        // The server validates the trimmed-or-empty value the same way the app does.
        var text = entry.key.trim();
        expect(text.isEmpty || server(text), entry.value);
      });
    }

    test('null is valid (the phone is optional)', () {
      expect(validate.phone(null), isNull);
    });
  });
}

// A connection failure, as the real client throws it.
class SocketExceptionLike implements Exception {
  const SocketExceptionLike();
}
