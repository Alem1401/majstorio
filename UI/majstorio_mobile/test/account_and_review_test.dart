import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:majstorio_mobile/providers/account/account_provider.dart';
import 'package:majstorio_mobile/screens/account/profile_edit_screen.dart';

import 'screens_test.dart' show app, signInAs;
import 'test_data.dart';

Map<String, dynamic> profileJson() => {
  'id': 5,
  'userName': 'mobile',
  'email': 'mobile@majstorio.test',
  'phoneNumber': null,
  'firstName': 'Amra',
  'lastName': 'Hadžić',
  'profileImageUrl': null,
  'isActive': true,
  'role': 'Customer',
};

void main() {
  setUp(() {
    AuthSession.clear();
  });

  testWidgets('profile edit loads the account and saves the changed details', (tester) async {
    signInAs(AuthSession.customer);
    var api = FakeApi((request) {
      if (request.method == 'PUT') return http.Response('', 200);
      return jsonResponse(profileJson());
    });

    await tester.pumpWidget(app(const ProfileEditScreen()));
    await tester.pumpAndSettle();
    expect(find.text('Amra'), findsOneWidget);

    await tester.enterText(find.widgetWithText(TextFormField, 'Telefon (nije obavezno)'), '+387 61 123 456');
    await tester.ensureVisible(find.text('Spremi'));
    await tester.tap(find.text('Spremi'));
    await tester.pumpAndSettle();

    var put = api.calls.singleWhere((call) => call.method == 'PUT');
    expect(put.url.path, '/Account');
    expect(put.body, contains('"phoneNumber":"+387 61 123 456"'));
    expect(put.body, contains('"email":"mobile@majstorio.test"'));
  });

  testWidgets('profile edit refuses an invalid e-mail without calling the API', (tester) async {
    signInAs(AuthSession.customer);
    var api = FakeApi((request) => jsonResponse(profileJson()));

    await tester.pumpWidget(app(const ProfileEditScreen()));
    await tester.pumpAndSettle();
    await tester.enterText(find.widgetWithText(TextFormField, 'E-pošta'), 'nije-email');
    await tester.ensureVisible(find.text('Spremi'));
    await tester.tap(find.text('Spremi'));
    await tester.pumpAndSettle();

    expect(find.text('E-pošta nije ispravna'), findsOneWidget);
    expect(api.calls.where((call) => call.method == 'PUT'), isEmpty);
  });

  test('photo upload is a multipart POST with the token', () async {
    AuthSession.token = 'abc';
    http.BaseRequest? sent;
    FakeApi((request) {
      sent = request;
      return http.Response('', 200);
    });

    await AccountProvider().uploadPhoto([0x89, 0x50, 0x4E, 0x47], 'slika.png');

    expect(sent!.method, 'POST');
    expect(sent!.url.path, '/Account/photo');
    expect(sent!.headers['Authorization'], 'Bearer abc');
    expect(sent!.headers['content-type'], startsWith('multipart/form-data'));
  });
}
