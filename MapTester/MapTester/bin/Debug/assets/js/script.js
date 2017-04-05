$(function(){
	ts = (new Date()).getTime() + 60*60*1000;		
	$('#countdown').countdown({
		timestamp	: ts,
		callback	: function(hours, minutes, seconds){
		}
	});
	
});
