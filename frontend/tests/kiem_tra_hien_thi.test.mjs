import test from 'node:test';
import assert from 'node:assert/strict';
import { docNgayUtc, gioVN, ngayVN, tien, nhanTrangThai, trangThaiMau, lay } from '../src/utils/hienThi.js';

test('SQL datetime UTC khong co timezone phai duoc doc thanh UTC', () => {
  assert.equal(docNgayUtc('2026-11-20T15:00:00').toISOString(), '2026-11-20T15:00:00.000Z');
  assert.equal(docNgayUtc('2026-11-20T15:00:00Z').toISOString(), '2026-11-20T15:00:00.000Z');
});
test('Hien thi gio Viet Nam UTC+7 va tien VND', () => {
  assert.match(gioVN('2026-11-20T15:00:00Z'), /22:00/);
  assert.equal(ngayVN('2026-11-20T15:00:00Z'), '20/11/2026');
  assert.match(tien(350000), /350\.000/);
});
test('Trang thai ve va ten cot Dapper hoat dong', () => {
  assert.equal(nhanTrangThai('PAID'), 'Đã thanh toán');
  assert.equal(trangThaiMau('HELD'), 'warning');
  assert.equal(lay({MaDatVe:123},'maDatVe'), 123);
});
