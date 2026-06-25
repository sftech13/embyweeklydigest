define(['baseView'], function (BaseView) {
    'use strict';

    var pluginId = 'c203ed6a-462b-40a6-87fa-ef58535a490d';

    function View(view, params) {
        BaseView.apply(this, arguments);

        view.querySelector('.btnSave').addEventListener('click', function () {
            onSaveClick(view);
        });

        view.querySelector('.btnSendNow').addEventListener('click', function () {
            onSendNowClick(view);
        });

        view.querySelector('.btnRefreshHistory').addEventListener('click', function () {
            loadHistory(view);
        });

        loadConfig(view);
        loadHistory(view);
    }

    View.prototype = Object.create(BaseView.prototype);
    View.prototype.constructor = View;

    function esc(str) {
        return String(str || '')
            .replace(/&/g, '&amp;').replace(/</g, '&lt;')
            .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    function showBox(el, msg, isError) {
        if (!el) return;
        el.textContent = msg;
        el.style.display = 'block';
        el.style.background = isError ? 'rgba(180,30,30,0.25)' : 'rgba(30,140,30,0.25)';
        el.style.color      = isError ? '#ff6b6b' : '#7ddc7d';
        el.style.border     = isError ? '1px solid rgba(180,30,30,0.5)' : '1px solid rgba(30,140,30,0.5)';
    }

    function loadConfig(view) {
        ApiClient.getPluginConfiguration(pluginId).then(function (config) {
            view.querySelector('.txtHeader').value      = config.Header || "What's New This Week";
            view.querySelector('.chkMovies').checked     = config.IncludeMovies !== false;
            view.querySelector('.chkSeries').checked     = config.IncludeSeries !== false;
            view.querySelector('.chkSkipEmpty').checked  = config.SkipWhenEmpty !== false;
            view.querySelector('.txtTimeout').value      = Math.round((config.TimeoutMs || 0) / 1000);
        });
    }

    function onSaveClick(view) {
        var btn = view.querySelector('.btnSave');
        if (btn) btn.disabled = true;

        ApiClient.getPluginConfiguration(pluginId).then(function (config) {
            config.Header         = view.querySelector('.txtHeader').value.trim() || "What's New This Week";
            config.IncludeMovies  = view.querySelector('.chkMovies').checked;
            config.IncludeSeries  = view.querySelector('.chkSeries').checked;
            config.SkipWhenEmpty  = view.querySelector('.chkSkipEmpty').checked;
            config.TimeoutMs      = (parseInt(view.querySelector('.txtTimeout').value, 10) || 0) * 1000;

            ApiClient.updatePluginConfiguration(pluginId, config).then(function () {
                if (btn) btn.disabled = false;
                showBox(view.querySelector('.saveStatus'), 'Settings saved.', false);
            }, function (err) {
                if (btn) btn.disabled = false;
                showBox(view.querySelector('.saveStatus'), 'Save failed: ' + (err.statusText || JSON.stringify(err)), true);
            });
        }, function (err) {
            if (btn) btn.disabled = false;
            showBox(view.querySelector('.saveStatus'), 'Could not load configuration: ' + (err.statusText || JSON.stringify(err)), true);
        });
    }

    function onSendNowClick(view) {
        var days = parseInt(view.querySelector('.txtLookback').value, 10) || 7;
        var btn = view.querySelector('.btnSendNow');
        if (btn) btn.disabled = true;
        showBox(view.querySelector('.sendStatus'), 'Building and sending digest…', false);

        ApiClient.ajax({
            type: 'POST',
            url: ApiClient.getUrl('EmbyWeeklyDigest/SendNow'),
            data: JSON.stringify({ LookbackDays: days }),
            contentType: 'application/json',
            dataType: 'json'
        }).then(function (result) {
            if (btn) btn.disabled = false;
            showBox(view.querySelector('.sendStatus'), result.Error || result.Message || 'Done.', !!result.Error);
            setTimeout(function () { loadHistory(view); }, 400);
        }, function (err) {
            if (btn) btn.disabled = false;
            showBox(view.querySelector('.sendStatus'), 'Error ' + (err.status || '') + ': ' + (err.statusText || JSON.stringify(err)), true);
        });
    }

    function timeAgo(isoStr) {
        var diff = Math.floor((Date.now() - new Date(isoStr).getTime()) / 1000);
        if (diff < 60)   return diff + 's ago';
        if (diff < 3600) return Math.floor(diff / 60) + 'm ago';
        if (diff < 86400) return Math.floor(diff / 3600) + 'h ago';
        return Math.floor(diff / 86400) + 'd ago';
    }

    function loadHistory(view) {
        ApiClient.ajax({
            type: 'GET',
            url: ApiClient.getUrl('EmbyWeeklyDigest/Digests'),
            dataType: 'json'
        }).then(function (data) {
            renderHistory(view, data);
        }, function () {
            var el = view.querySelector('.historyList');
            if (el) el.innerHTML = '<p style="opacity:.4;font-size:.85em;">Could not load history.</p>';
        });
    }

    function renderHistory(view, data) {
        var el = view.querySelector('.historyList');
        if (!el) return;

        var active = (data || []).filter(function (n) { return n.Active; });

        if (active.length === 0) {
            el.innerHTML = '<p style="opacity:.35;font-size:.85em;margin:0;">No active digests.</p>';
            return;
        }

        el.innerHTML = '';
        active.forEach(function (n) {
            var deliveries = n.Deliveries || {};
            var delivered  = Object.keys(deliveries);

            var badgeHtml = '';
            delivered.forEach(function (uid) {
                var rec = deliveries[uid];
                badgeHtml += '<span style="display:inline-block;font-size:.72em;padding:.2em .55em;border-radius:99px;'
                    + 'background:rgba(30,140,30,0.2);color:#7ddc7d;border:1px solid rgba(30,140,30,0.35);margin:.15em;" '
                    + 'title="' + esc(timeAgo(rec.DeliveredAt)) + '">'
                    + esc(rec.Username) + ' ✓</span>';
            });

            badgeHtml += '<span style="display:inline-block;font-size:.72em;padding:.2em .55em;border-radius:99px;'
                + 'background:rgba(180,140,0,0.18);color:#e0b830;border:1px solid rgba(180,140,0,0.35);margin:.15em;">'
                + (delivered.length === 0 ? 'No deliveries yet' : 'Pending offline users') + '</span>';

            var item = document.createElement('div');
            item.style.cssText = 'border:1px solid rgba(255,255,255,0.08);border-radius:6px;padding:.75em 1em;margin-bottom:.6em;background:rgba(0,0,0,0.2);';
            item.innerHTML =
                '<div style="display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:.3em;">'
                + '<span style="font-weight:600;font-size:.95em;">' + esc(n.Header) + '</span>'
                + '<span style="font-size:.75em;opacity:.4;">' + esc(timeAgo(n.CreatedAt)) + '</span>'
                + '</div>'
                + '<div style="font-size:.88em;opacity:.75;margin-bottom:.5em;white-space:pre-line;">' + esc(n.Text) + '</div>'
                + '<div style="margin-bottom:.5em;">' + badgeHtml + '</div>'
                + '<button class="dismissBtn" data-id="' + esc(n.Id) + '" '
                + 'style="font-size:.75em;color:rgba(255,255,255,0.3);background:none;border:1px solid rgba(255,255,255,0.1);'
                + 'border-radius:4px;padding:.2em .6em;cursor:pointer;">Dismiss</button>';

            el.appendChild(item);
        });

        el.querySelectorAll('.dismissBtn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = btn.getAttribute('data-id');
                btn.disabled = true;
                btn.textContent = '…';
                ApiClient.ajax({
                    type: 'DELETE',
                    url: ApiClient.getUrl('EmbyWeeklyDigest/Digests/' + id)
                }).then(function () {
                    loadHistory(view);
                });
            });
        });
    }

    return View;
});
