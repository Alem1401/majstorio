import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:majstorio_mobile/providers/appointment/appointment_provider.dart';
import 'package:majstorio_mobile/providers/auth/auth_provider.dart';
import 'package:majstorio_mobile/providers/category/category_provider.dart';
import 'package:majstorio_mobile/providers/city/city_provider.dart';
import 'package:majstorio_mobile/providers/news_post/news_provider.dart';
import 'package:majstorio_mobile/providers/offer/offer_provider.dart';
import 'package:majstorio_mobile/providers/service_request/service_request_provider.dart';
import 'package:majstorio_mobile/providers/tradesperson_profile/tradesperson_profile_provider.dart';
import 'package:majstorio_mobile/screens/login_screen.dart';
import 'package:majstorio_mobile/screens/offer/offer_details_screen.dart';
import 'package:majstorio_mobile/screens/service_request/service_request_details_screen.dart';
import 'package:majstorio_mobile/models/service_request/service_request.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:provider/provider.dart';

import 'test_data.dart';

// The app's providers around a screen, the same way main.dart sets them up.
Widget app(Widget screen) {
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
    ],
    child: MaterialApp(theme: AppTheme.light, home: screen),
  );
}

void signInAs(String role, {int userId = 5}) {
  AuthSession.token = 'abc';
  AuthSession.role = role;
  AuthSession.userId = userId;
  AuthSession.userName = 'test';
}

void main() {
  setUp(() {
    AuthSession.clear();
  });

  testWidgets('login checks the fields before calling the API', (tester) async {
    var api = FakeApi((request) => http.Response('', 401));
    await tester.pumpWidget(app(const LoginScreen()));

    await tester.tap(find.text('Prijavi se'));
    await tester.pump();

    expect(find.text('Korisničko ime je obavezno'), findsOneWidget);
    expect(find.text('Lozinka je obavezna'), findsOneWidget);
    expect(api.calls, isEmpty);
  });

  testWidgets('owner sees edit and delete on a request without offers', (tester) async {
    signInAs(AuthSession.customer);
    FakeApi((request) => jsonResponse(requestDetailJson()));

    await tester.pumpWidget(app(const ServiceRequestDetailsScreen(requestId: 10)));
    await tester.pumpAndSettle();

    expect(find.text('Uredi zahtjev'), findsOneWidget);
    expect(find.text('Obriši zahtjev'), findsOneWidget);
    expect(find.text('Otkaži zahtjev'), findsOneWidget);
    expect(find.textContaining('Usporedi ponude'), findsNothing);
  });

  testWidgets('owner with pending offers can compare them but not edit', (tester) async {
    signInAs(AuthSession.customer);
    FakeApi((request) => jsonResponse(requestDetailJson(offers: [offerJson(id: 1), offerJson(id: 2, price: 70)])));

    await tester.pumpWidget(app(const ServiceRequestDetailsScreen(requestId: 10)));
    await tester.pumpAndSettle();

    expect(find.text('Usporedi ponude (2)'), findsOneWidget);
    expect(find.text('Uredi zahtjev'), findsNothing);
    expect(find.text('Obriši zahtjev'), findsNothing);
  });

  testWidgets('delete asks first and calls the API only after "Da"', (tester) async {
    signInAs(AuthSession.customer);
    var api = FakeApi((request) {
      if (request.method == 'DELETE') return http.Response('', 200);
      return jsonResponse(requestDetailJson());
    });

    await tester.pumpWidget(app(const ServiceRequestDetailsScreen(requestId: 10)));
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.text('Obriši zahtjev'));
    await tester.tap(find.text('Obriši zahtjev'));
    await tester.pumpAndSettle();
    expect(find.text('Brisanje zahtjeva'), findsOneWidget);

    await tester.tap(find.text('Ne'));
    await tester.pumpAndSettle();
    expect(api.calls.where((call) => call.method == 'DELETE'), isEmpty);
  });

  testWidgets('another customer sees no actions on a request', (tester) async {
    signInAs(AuthSession.customer, userId: 99);
    FakeApi((request) => jsonResponse(requestDetailJson()));

    await tester.pumpWidget(app(const ServiceRequestDetailsScreen(requestId: 10)));
    await tester.pumpAndSettle();

    expect(find.text('Uredi zahtjev'), findsNothing);
    expect(find.text('Otkaži zahtjev'), findsNothing);
  });

  testWidgets('a hidden request shows a neutral message', (tester) async {
    signInAs(AuthSession.customer);
    FakeApi((request) => http.Response('', 404));

    await tester.pumpWidget(app(const ServiceRequestDetailsScreen(requestId: 10)));
    await tester.pumpAndSettle();

    expect(find.text('Ovaj zahtjev nije dostupan.'), findsOneWidget);
  });

  testWidgets('cancel sends the entered reason and shows the result', (tester) async {
    signInAs(AuthSession.customer);
    var status = 0;
    var api = FakeApi((request) {
      if (request.method == 'POST') {
        status = 4;
        return http.Response('', 200);
      }
      return jsonResponse(requestDetailJson(status: status));
    });

    await tester.pumpWidget(app(const ServiceRequestDetailsScreen(requestId: 10)));
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.text('Otkaži zahtjev'));
    await tester.tap(find.text('Otkaži zahtjev'));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField), 'Ne treba više');
    await tester.tap(find.text('Da'));
    await tester.pumpAndSettle();

    var cancel = api.calls.singleWhere((call) => call.method == 'POST');
    expect(cancel.url.path, '/ServiceRequests/10/cancel');
    expect(cancel.body, '{"reason":"Ne treba više"}');
    expect(find.text('Zahtjev je otkazan.'), findsOneWidget);
    expect(find.text('Otkaži zahtjev'), findsNothing);
  });

  testWidgets('tradesperson can send an offer on an open request, and withdraw a pending one', (tester) async {
    signInAs(AuthSession.tradesperson, userId: 8);
    var offers = <Map<String, dynamic>>[];
    FakeApi((request) => jsonResponse(requestDetailJson(offers: offers)));

    await tester.pumpWidget(app(const ServiceRequestDetailsScreen(requestId: 10)));
    await tester.pumpAndSettle();
    expect(find.text('Pošalji ponudu'), findsOneWidget);

    offers.add(offerJson(id: 1));
    await tester.pumpWidget(app(const ServiceRequestDetailsScreen(key: ValueKey(11), requestId: 11)));
    await tester.pumpAndSettle();
    expect(find.text('Pošalji ponudu'), findsNothing);
    expect(find.text('Izmijeni ponudu'), findsOneWidget);
    expect(find.text('Povuci ponudu'), findsOneWidget);
  });

  testWidgets('assigned tradesperson cannot complete before the start', (tester) async {
    signInAs(AuthSession.tradesperson, userId: 8);
    FakeApi((request) => jsonResponse(requestDetailJson(
      status: ServiceRequest.appointmentConfirmed,
      offers: [offerJson(id: 1, status: 1)],
      appointment: appointmentJson(start: DateTime.now().toUtc().add(const Duration(days: 2))),
    )));

    await tester.pumpWidget(app(const ServiceRequestDetailsScreen(requestId: 10)));
    await tester.pumpAndSettle();

    var button = tester.widget<ElevatedButton>(find.widgetWithText(ElevatedButton, 'Završi posao'));
    expect(button.onPressed, isNull);
    expect(find.textContaining('nakon početka termina'), findsOneWidget);
  });

  testWidgets('offer form refuses a price above the budget', (tester) async {
    signInAs(AuthSession.tradesperson, userId: 8);
    var api = FakeApi((request) => http.Response('', 200));
    var request = ServiceRequest.fromJson(requestJson());

    await tester.pumpWidget(app(OfferDetailsScreen(request: request)));
    await tester.enterText(find.widgetWithText(TextFormField, 'Cijena (EUR)'), '150');
    await tester.enterText(find.widgetWithText(TextFormField, 'Trajanje (minuta)'), '60');
    await tester.ensureVisible(find.text('Pošalji ponudu'));
    await tester.tap(find.text('Pošalji ponudu'));
    await tester.pumpAndSettle();

    expect(find.textContaining('ne smije biti veći od budžeta'), findsOneWidget);
    expect(api.calls, isEmpty);
  });
}
